using Android.Content;
using Android.OS;
using Android.Security.Keystore;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using GDSB.Domain.Interfaces;
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

        public Task<bool> IsAvailableAsync()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                return Task.FromResult(false);

            var manager = BiometricManager.From(global::Android.App.Application.Context);
            var canAuthenticate = manager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong);
            return Task.FromResult(canAuthenticate == BiometricManager.BiometricSuccess);
        }

        public Task<bool> IsEnabledAsync() =>
            Task.FromResult(GetPrefs().Contains(CiphertextPrefKey));

        public async Task<bool> StoreKeyAsync(byte[] derivedKey)
        {
            if (GetActivity() is not { } activity)
                return false;

            try
            {
                var key = GetOrCreateKey();
                var cipher = Cipher.GetInstance(Transformation)!;
                cipher.Init(CipherMode.EncryptMode, key);

                var authenticatedCipher = await AuthenticateAsync(activity, cipher, "Confirme sua biometria para ativar o desbloqueio rápido");
                if (authenticatedCipher is null)
                    return false;

                var ciphertext = authenticatedCipher.DoFinal(derivedKey);
                var iv = authenticatedCipher.GetIV();

                var editor = GetPrefs().Edit()!;
                editor.PutString(IvPrefKey, Convert.ToBase64String(iv!));
                editor.PutString(CiphertextPrefKey, Convert.ToBase64String(ciphertext!));
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
            var ivBase64 = prefs.GetString(IvPrefKey, null);
            var ciphertextBase64 = prefs.GetString(CiphertextPrefKey, null);
            if (ivBase64 is null || ciphertextBase64 is null)
                return null;

            try
            {
                var keyStore = KeyStore.GetInstance(KeystoreProvider)!;
                keyStore.Load(null);

                if (keyStore.GetKey(KeyAlias, null) is not { } key)
                    return null;

                var cipher = Cipher.GetInstance(Transformation)!;
                var iv = Convert.FromBase64String(ivBase64);
                cipher.Init(CipherMode.DecryptMode, key, new GCMParameterSpec(GcmTagLengthBits, iv));

                var authenticatedCipher = await AuthenticateAsync(activity, cipher, "Use sua biometria para abrir o cofre");
                if (authenticatedCipher is null)
                    return null;

                var ciphertext = Convert.FromBase64String(ciphertextBase64);
                return authenticatedCipher.DoFinal(ciphertext);
            }
            catch (KeyPermanentlyInvalidatedException)
            {
                // Chave invalidada (ex.: nova digital cadastrada) - o segredo selado já ficou
                // inútil pra sempre; limpa e cai de volta pro campo de senha sem tratamento
                // especial, como decidido no plano da Fase 5.
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
            GetPrefs().Edit()?.Clear()?.Apply();

            try
            {
                var keyStore = KeyStore.GetInstance(KeystoreProvider)!;
                keyStore.Load(null);
                if (keyStore.IsKeyEntry(KeyAlias))
                    keyStore.DeleteEntry(KeyAlias);
            }
            catch (Exception)
            {
                // Nada a fazer se o Keystore já não tiver a entrada - o objetivo é só garantir
                // que não sobra segredo selado utilizável.
            }

            return Task.CompletedTask;
        }

        private static FragmentActivity? GetActivity() => Platform.CurrentActivity as FragmentActivity;

        private static global::Android.Content.ISharedPreferences GetPrefs() =>
            global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;

        private static IKey GetOrCreateKey()
        {
            var keyStore = KeyStore.GetInstance(KeystoreProvider)!;
            keyStore.Load(null);

            if (keyStore.IsKeyEntry(KeyAlias) && keyStore.GetKey(KeyAlias, null) is { } existingKey)
                return existingKey;

            var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeystoreProvider)!;
            var spec = new KeyGenParameterSpec.Builder(KeyAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                .SetUserAuthenticationRequired(true)
                .Build();

            keyGenerator.Init(spec);
            return keyGenerator.GenerateKey()!;
        }

        // Devolve o Cipher já autorizado (mesma instância passada em CryptoObject) depois de uma
        // biometria bem-sucedida, ou null se o usuário cancelar/errar ou o prompt der erro.
        private static Task<Cipher?> AuthenticateAsync(FragmentActivity activity, Cipher cipher, string subtitle)
        {
            var tcs = new TaskCompletionSource<Cipher?>();

            activity.RunOnUiThread(() =>
            {
                var executor = ContextCompat.GetMainExecutor(activity)!;
                var prompt = new BiometricPrompt(activity, executor, new AuthCallback(tcs));

                var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("GDSB")
                    .SetSubtitle(subtitle)
                    .SetNegativeButtonText("Usar senha")
                    .SetAllowedAuthenticators((int)BiometricManager.Authenticators.BiometricStrong)
                    .Build();

                prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
            });

            return tcs.Task;
        }

        private sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
        {
            private readonly TaskCompletionSource<Cipher?> _tcs;

            public AuthCallback(TaskCompletionSource<Cipher?> tcs) => _tcs = tcs;

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
                _tcs.TrySetResult(result.CryptoObject?.Cipher);

            public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString) =>
                _tcs.TrySetResult(null);

            // Não resolve a task aqui: uma tentativa falha (dedo errado) deixa o prompt aberto pro
            // usuário tentar de novo - só OnAuthenticationSucceeded/OnAuthenticationError encerram.
            public override void OnAuthenticationFailed()
            {
            }
        }
    }
}
