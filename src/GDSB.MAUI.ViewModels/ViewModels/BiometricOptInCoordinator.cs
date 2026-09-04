using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;
using System.Security.Cryptography;
using System.Text;

namespace GDSB.MAUI.ViewModels
{
    // ViewModel da BiometricOptInView (Views/BiometricOptInView.xaml). O mesmo diálogo de opt-in
    // de biometria é oferecido tanto depois de abrir um cofre existente (UnlockViewModel) quanto
    // depois de criar um novo (CreateVaultViewModel) - por isso vive num lugar só em vez de
    // duplicado nos dois, incluindo as chaves de Preferences que amarram a biometria a um cofre
    // específico (ver RememberVault/ForgetVault).
    public partial class BiometricOptInCoordinator : LocalizedObject
    {
        public const string PromptedPreferenceKey = "gdsb.biometricPrompted";
        public const string LastLocationPreferenceKey = "gdsb.lastVaultLocation";
        public const string LastVaultNamePreferenceKey = "gdsb.lastVaultName";

        // Deixa de ser const porque passa a vir do catálogo (ILocalizationService), que resolve na
        // cultura vigente a cada leitura.
        private string UnavailableMessage => Localization.Get("BiometricOptIn_UnavailableMessage");

        private readonly IBiometricUnlockService _biometricUnlockService;
        private readonly IAlertService _alertService;
        private readonly IPreferencesService _preferencesService;

        private TaskCompletionSource<bool>? _response;

        public BiometricOptInCoordinator(
            IBiometricUnlockService biometricUnlockService,
            IAlertService alertService,
            IPreferencesService preferencesService,
            ILocalizationService localizationService)
            : base(localizationService)
        {
            _biometricUnlockService = biometricUnlockService;
            _alertService = alertService;
            _preferencesService = preferencesService;
        }

        [ObservableProperty]
        private bool isVisible;

        // Grava qual cofre a biometria deve mirar da próxima vez - chamado a cada Open/CreateVault
        // bem-sucedido, esteja a biometria ativa ou não (é só o "candidato" a próximo alvo).
        public void RememberVault(string location, string vaultName)
        {
            _preferencesService.SetString(LastLocationPreferenceKey, location);
            _preferencesService.SetString(LastVaultNamePreferenceKey, vaultName);
        }

        // Esquece o cofre-alvo e a resposta já dada ao opt-in - usado ao trocar de cofre (manual
        // ou criando um novo), pra que o próximo Open/CreateVault bem-sucedido ofereça o opt-in de
        // novo, já mirando o cofre certo. Não mexe no segredo selado em si - quem chama isso é
        // responsável por também chamar IBiometricUnlockService.DisableAsync.
        public void ForgetVault()
        {
            _preferencesService.Remove(LastLocationPreferenceKey);
            _preferencesService.Remove(LastVaultNamePreferenceKey);
            _preferencesService.Remove(PromptedPreferenceKey);
        }

        // Só pergunta uma vez na vida do app (por dispositivo) - nem repete a pergunta se o
        // usuário recusar (ver PromptedPreferenceKey). Chamado só depois de um Open/CreateVault
        // bem-sucedido, nunca a partir do próprio atalho de biometria.
        public async Task MaybeOfferAsync(string password)
        {
            if (_preferencesService.GetBool(PromptedPreferenceKey, false))
                return;

            if (!await _biometricUnlockService.IsAvailableAsync() || await _biometricUnlockService.IsEnabledAsync())
                return;

            _preferencesService.SetBool(PromptedPreferenceKey, true);

            _response = new TaskCompletionSource<bool>();
            IsVisible = true;
            var accepted = await _response.Task;

            if (!accepted)
                return;

            var secret = Encoding.UTF8.GetBytes(password);
            try
            {
                var stored = await _biometricUnlockService.StoreKeyAsync(secret);
                if (!stored)
                    await _alertService.DisplayAlertAsync(null, UnavailableMessage, Localization.Get("Common_Ok"));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        [RelayCommand]
        private void Accept()
        {
            IsVisible = false;
            _response?.TrySetResult(true);
        }

        [RelayCommand]
        private void Decline()
        {
            IsVisible = false;
            _response?.TrySetResult(false);
        }
    }
}
