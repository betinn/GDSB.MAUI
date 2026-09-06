using Android.Content;
using Android.OS;
using Android.Security.Keystore;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Services;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Microsoft.Maui.ApplicationModel;

namespace GDSB.MAUI.Platforms.Android.Services
{
    // O "segredo selado" nunca é a chave de criptografia do cofre em si (essa é derivada por
    // arquivo, com PBKDF2 + salt próprio - ver AesGcmFileCryptoService): é a senha mestra, cifrada
    // com uma chave AES que mora só no Android Keystore, gerada com SetUserAuthenticationRequired
    // (true). Isso significa que nem o próprio app consegue usar essa chave sem antes passar pelo
    // BiometricPrompt de novo - a senha cifrada guardada em SharedPreferences é inútil sem isso.
    public class BiometricUnlockService : IBiometricUnlockService
    {
        private const string KeyAlias = "gdsb_biometric_unlock_key";
        private const string KeystoreProvider = "AndroidKeyStore";
        private const string Transformation = KeyProperties.KeyAlgorithmAes + "/" + KeyProperties.BlockModeGcm + "/" + KeyProperties.EncryptionPaddingNone;
        private const int GcmTagLengthBits = 128;

        private const string PrefsName = "gdsb_biometric";
        private const string IvPrefKey = "iv";
        private const string CiphertextPrefKey = "ciphertext";

        private readonly ILocalizationService _localization;

        public BiometricUnlockService(ILocalizationService localization)
        {
            _localization = localization;
        }

        public Task<bool> IsAvailableAsync()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                return Task.FromResult(false);

            var manager = BiometricManager.From(global::Android.App.Application.Context);
            var canAuthenticate = manager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong);
            return Task.FromResult(canAuthenticate == BiometricManager.BiometricSuccess);
        }

        public Task<bool> IsEnabledAsync() =>
            Task.FromResult(GetPrefs() is { } prefs && prefs.Contains(CiphertextPrefKey));

        public async Task<bool> StoreKeyAsync(byte[] derivedKey)
        {
            if (GetActivity() is not { } activity)
                return false;

            try
            {
                var key = GetOrCreateKey();
                var cipher = Cipher.GetInstance(Transformation);
                if (key is null || cipher is null)
                    return false;

                cipher.Init(CipherMode.EncryptMode, key);

                var authenticatedCipher = await AuthenticateAsync(
                    activity, cipher, _localization.Get("Platform_BiometricActivateSubtitle"), _localization.Get("Platform_BiometricNegativeButton"));
                if (authenticatedCipher is null)
                    return false;

                var ciphertext = authenticatedCipher.DoFinal(derivedKey);
                var iv = authenticatedCipher.GetIV();

                // Sem ciphertext ou sem IV não há segredo selado utilizável: melhor não gravar
                // nada e deixar a biometria desligada do que guardar metade do par.
                if (ciphertext is null || iv is null)
                    return false;

                var editor = GetPrefs()?.Edit();
                if (editor is null)
                    return false;

                editor.PutString(IvPrefKey, Convert.ToBase64String(iv));
                editor.PutString(CiphertextPrefKey, Convert.ToBase64String(ciphertext));
                editor.Apply();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<byte[]?> TryUnlockAsync()
        {
            if (GetActivity() is not { } activity)
                return null;

            var prefs = GetPrefs();
            if (prefs is null)
                return null;

            var ivBase64 = prefs.GetString(IvPrefKey, null);
            var ciphertextBase64 = prefs.GetString(CiphertextPrefKey, null);
            if (ivBase64 is null || ciphertextBase64 is null)
                return null;

            try
            {
                var keyStore = KeyStore.GetInstance(KeystoreProvider);
                if (keyStore is null)
                    return null;

                keyStore.Load(null);

                if (keyStore.GetKey(KeyAlias, null) is not { } key)
                    return null;

                var cipher = Cipher.GetInstance(Transformation);
                if (cipher is null)
                    return null;

                var iv = Convert.FromBase64String(ivBase64);
                cipher.Init(CipherMode.DecryptMode, key, new GCMParameterSpec(GcmTagLengthBits, iv));

                var authenticatedCipher = await AuthenticateAsync(
                    activity, cipher, _localization.Get("Platform_BiometricUnlockSubtitle"), _localization.Get("Platform_BiometricNegativeButton"));
                if (authenticatedCipher is null)
                    return null;

                var ciphertext = Convert.FromBase64String(ciphertextBase64);
                return authenticatedCipher.DoFinal(ciphertext);
            }
            catch (KeyPermanentlyInvalidatedException)
            {
                // Chave invalidada (ex.: nova digital cadastrada) - o segredo selado já ficou
                // inútil pra sempre; limpa e cai de volta pro campo de senha sem tratamento
                // especial.
                await DisableAsync();
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task DisableAsync()
        {
            GetPrefs()?.Edit()?.Clear()?.Apply();

            try
            {
                var keyStore = KeyStore.GetInstance(KeystoreProvider);
                if (keyStore is not null)
                {
                    keyStore.Load(null);
                    if (keyStore.IsKeyEntry(KeyAlias))
                        keyStore.DeleteEntry(KeyAlias);
                }
            }
            catch (Exception)
            {
                // Nada a fazer se o Keystore já não tiver a entrada - o objetivo é só garantir
                // que não sobra segredo selado utilizável.
            }

            return Task.CompletedTask;
        }

        private static FragmentActivity? GetActivity() => Platform.CurrentActivity as FragmentActivity;

        // Devolve null quando a plataforma não entrega as preferências (ou o Keystore não abre):
        // quem chama trata como "biometria indisponível" e cai de volta pro campo de senha, o
        // mesmo caminho de um cancelamento no prompt.
        private static global::Android.Content.ISharedPreferences? GetPrefs() =>
            global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private);

        private static IKey? GetOrCreateKey()
        {
            var keyStore = KeyStore.GetInstance(KeystoreProvider);
            if (keyStore is null)
                return null;

            keyStore.Load(null);

            if (keyStore.IsKeyEntry(KeyAlias) && keyStore.GetKey(KeyAlias, null) is { } existingKey)
                return existingKey;

            var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeystoreProvider);
            if (keyGenerator is null)
                return null;

            var spec = new KeyGenParameterSpec.Builder(KeyAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                .SetUserAuthenticationRequired(true)
                .Build();

            keyGenerator.Init(spec);
            return keyGenerator.GenerateKey();
        }

        // Devolve o Cipher já autorizado (mesma instância passada em CryptoObject) depois de uma
        // biometria bem-sucedida, ou null se o usuário cancelar/errar ou o prompt der erro.
        // negativeButtonText entra como parâmetro (em vez de ler _localization aqui) só para poder
        // continuar static, como o resto dos métodos auxiliares desta classe.
        private static async Task<Cipher?> AuthenticateAsync(FragmentActivity activity, Cipher cipher, string subtitle, string negativeButtonText)
        {
            // BiometricPrompt.Authenticate chamado antes da janela ter foco (ex.: disparado sozinho
            // assim que a UnlockPage aparece, seja na abertura do app ou voltando de background)
            // falha silenciosamente ou nunca chega a aparecer - só funcionava de verdade quando o
            // usuário tocava no botão manualmente, porque a essa altura a janela já tinha foco há
            // tempo. Esperar aqui cobre os dois casos com o mesmo código.
            await WaitForWindowFocusAsync(activity);

            var tcs = new TaskCompletionSource<Cipher?>();

            activity.RunOnUiThread(() =>
            {
                var executor = ContextCompat.GetMainExecutor(activity);
                if (executor is null)
                {
                    // Sem executor não há como o BiometricPrompt entregar o resultado: resolve a
                    // task aqui mesmo, senão o chamador esperaria pra sempre por um prompt que
                    // nunca vai aparecer.
                    tcs.TrySetResult(null);
                    return;
                }

                var prompt = new BiometricPrompt(activity, executor, new AuthCallback(tcs));

                var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("GDSB")
                    .SetSubtitle(subtitle)
                    .SetNegativeButtonText(negativeButtonText)
                    .SetAllowedAuthenticators((int)BiometricManager.Authenticators.BiometricStrong)
                    .Build();

                prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
            });

            return await tcs.Task;
        }

        private static readonly TimeSpan WindowFocusTimeout = TimeSpan.FromSeconds(5);

        // Se a janela já está em foco (o caso comum: usuário tocou no botão), retorna na hora.
        // Senão, espera o próximo MainActivity.WindowFocusChanged(true) - com um teto de 5s pra
        // nunca travar pra sempre num cenário inesperado (nesse caso o prompt tenta aparecer sem
        // foco garantido mesmo, o mesmo comportamento de antes dessa correção).
        private static Task WaitForWindowFocusAsync(FragmentActivity activity)
        {
            if (activity.HasWindowFocus)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource();
            Action<bool>? handler = null;
            handler = hasFocus =>
            {
                if (!hasFocus)
                    return;

                MainActivity.WindowFocusChanged -= handler;
                tcs.TrySetResult();
            };
            MainActivity.WindowFocusChanged += handler;

            return Task.WhenAny(tcs.Task, Task.Delay(WindowFocusTimeout))
                .ContinueWith(_ => MainActivity.WindowFocusChanged -= handler, TaskScheduler.Default);
        }

        private sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
        {
            private readonly TaskCompletionSource<Cipher?> _tcs;

            public AuthCallback(TaskCompletionSource<Cipher?> tcs) => _tcs = tcs;

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
                _tcs.TrySetResult(result.CryptoObject?.Cipher);

            // errorCode/errString não são usados: qualquer erro (cancelamento, timeout, bloqueio
            // por tentativas) tem o mesmo tratamento aqui - falha e deixa o chamador cair pro
            // campo de senha. Assinatura imposta pela classe base do AndroidX.Biometric (override),
            // então os parâmetros não podem ser removidos - o prefixo "_" não basta pro Sonar, daí a
            // supressão explícita abaixo.
#pragma warning disable S1172
            public override void OnAuthenticationError(int _errorCode, Java.Lang.ICharSequence _errString) =>
                _tcs.TrySetResult(null);
#pragma warning restore S1172

            // Não resolve a task aqui: uma tentativa falha (dedo errado) deixa o prompt aberto pro
            // usuário tentar de novo - só OnAuthenticationSucceeded/OnAuthenticationError encerram.
            // Corpo vazio é intencional: não há ação de sistema a tomar numa falha recuperável.
            public override void OnAuthenticationFailed()
            {
            }
        }
    }
}
