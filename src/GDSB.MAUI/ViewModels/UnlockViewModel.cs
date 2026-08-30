using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

// VaultPage vive no namespace raiz GDSB.MAUI (namespace mãe deste), resolvida sem using extra.
namespace GDSB.MAUI.ViewModels
{
    public partial class UnlockViewModel : ObservableObject
    {
        // Senha errada e arquivo corrompido devem ser indistinguíveis pra quem usa o app -
        // nunca mostrar ex.Message cru, sempre essa mensagem genérica.
        private const string GenericErrorMessage = "Senha incorreta ou arquivo corrompido.";
        private const string EmptyPasswordMessage = "Digite a senha mestra do cofre.";
        private const string FilePickerErrorMessage = "Não foi possível abrir o seletor de arquivos.";
        private const string BiometricUnavailableMessage = "Não foi possível usar a biometria. Digite a senha mestra.";

        // Amarrado ao último cofre aberto: não existe seleção de perfil, um uso real é um cofre
        // por aparelho, então guardar só a última location basta. Preferences é usado direto aqui
        // (sem uma interface própria) pelo mesmo motivo de Launcher em VaultViewModel.OpenUrlAsync:
        // é um detalhe de sessão, não algo que precisa de mock nos fluxos já cobertos por teste.
        private const string LastLocationPreferenceKey = "gdsb.lastVaultLocation";
        private const string BiometricPromptedPreferenceKey = "gdsb.biometricPrompted";

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IAlertService _alertService;
        private readonly IBiometricUnlockService _biometricUnlockService;

        public UnlockViewModel(
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IAlertService alertService,
            IBiometricUnlockService biometricUnlockService)
        {
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _alertService = alertService;
            _biometricUnlockService = biometricUnlockService;
        }

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isPasswordHidden = true;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool canUseBiometric;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;

        public string UnlockButtonText => IsBusy ? "Abrindo..." : "Abrir cofre";

        public string EyeGlyph => IsPasswordHidden ? "👁" : "🙈";

        [RelayCommand]
        private void ToggleShowPassword() => IsPasswordHidden = !IsPasswordHidden;

        [RelayCommand]
        private Task GoToCreateVaultAsync() => _navigationService.NavigateToAsync(nameof(CreateVaultPage));

        public void ClearPassword()
        {
            Password = string.Empty;
            ErrorMessage = null;
            IsPasswordHidden = true;
        }

        // Chamado no OnAppearing da UnlockPage - decide se mostra o atalho de biometria: precisa
        // de sensor disponível, de já estar habilitado (StoreKeyAsync feito numa sessão anterior)
        // e de haver uma location de cofre pra reabrir.
        public async Task RefreshBiometricAvailabilityAsync()
        {
            var lastLocation = Preferences.Default.Get<string?>(LastLocationPreferenceKey, null);
            if (string.IsNullOrEmpty(lastLocation))
            {
                CanUseBiometric = false;
                return;
            }

            CanUseBiometric = await _biometricUnlockService.IsAvailableAsync()
                && await _biometricUnlockService.IsEnabledAsync();
        }

        [RelayCommand(CanExecute = nameof(CanUnlock))]
        private async Task UnlockAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = EmptyPasswordMessage;
                return;
            }

            string? location;
            try
            {
                location = await _filePickerService.PickFileNameAsync();
            }
            catch (Exception)
            {
                ErrorMessage = FilePickerErrorMessage;
                return;
            }

            if (string.IsNullOrEmpty(location))
                return;

            await OpenAndNavigateAsync(location, Password, offerBiometricOptIn: true);
        }

        [RelayCommand(CanExecute = nameof(CanUnlock))]
        private async Task UnlockWithBiometricAsync()
        {
            var lastLocation = Preferences.Default.Get<string?>(LastLocationPreferenceKey, null);
            if (string.IsNullOrEmpty(lastLocation))
                return;

            IsBusy = true;
            byte[]? secret;
            try
            {
                secret = await _biometricUnlockService.TryUnlockAsync();
            }
            catch (Exception)
            {
                secret = null;
            }
            finally
            {
                IsBusy = false;
            }

            if (secret is null)
            {
                // Cancelado pelo usuário, sensor indisponível ou chave invalidada (ex.: nova
                // digital cadastrada) - cai de volta pro campo de senha sem alarde nenhum.
                await RefreshBiometricAvailabilityAsync();
                return;
            }

            string recoveredPassword;
            try
            {
                recoveredPassword = Encoding.UTF8.GetString(secret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }

            await OpenAndNavigateAsync(lastLocation, recoveredPassword, offerBiometricOptIn: false);
        }

        private async Task OpenAndNavigateAsync(string location, string enteredPassword, bool offerBiometricOptIn)
        {
            IsBusy = true;
            try
            {
                var result = await Task.Run(() => _profileFileService.Open(location, enteredPassword));

                if (result.WasLegacyFormat)
                    await Task.Run(() => _profileFileService.Save(location, result.Profile, enteredPassword));

                Preferences.Default.Set(LastLocationPreferenceKey, location);

                if (offerBiometricOptIn)
                    await MaybeOfferBiometricOptInAsync(enteredPassword);

                ClearPassword();

                await _navigationService.NavigateToRootAsync(nameof(VaultPage), new Dictionary<string, object>
                {
                    ["Profile"] = result.Profile,
                    ["Location"] = location,
                    ["Password"] = enteredPassword,
                });
            }
            catch (Exception)
            {
                // Cobre tanto InvalidPasswordOrCorruptFileException (v2) quanto as exceções do
                // leitor legado (v1) - a mensagem pro usuário é sempre a mesma, de propósito. Vale
                // também pra location vinda do atalho de biometria (arquivo movido/removido).
                ErrorMessage = GenericErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Só pergunta uma vez na vida do app (por dispositivo) - nem repete a pergunta se o
        // usuário recusar. Chamado só depois de um Open manual bem-sucedido, nunca a partir do
        // próprio atalho de biometria.
        private async Task MaybeOfferBiometricOptInAsync(string password)
        {
            if (Preferences.Default.Get(BiometricPromptedPreferenceKey, false))
                return;

            if (!await _biometricUnlockService.IsAvailableAsync() || await _biometricUnlockService.IsEnabledAsync())
                return;

            Preferences.Default.Set(BiometricPromptedPreferenceKey, true);

            var accepted = await _alertService.DisplayConfirmAsync(
                "Desbloqueio rápido",
                "Usar biometria para abrir este cofre da próxima vez? A senha mestra continua funcionando normalmente.",
                "Usar biometria",
                "Agora não");

            if (!accepted)
                return;

            var secret = Encoding.UTF8.GetBytes(password);
            try
            {
                var stored = await _biometricUnlockService.StoreKeyAsync(secret);
                if (!stored)
                    await _alertService.DisplayAlertAsync(null, BiometricUnavailableMessage, "Ok");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        private bool CanUnlock() => !IsBusy;

        partial void OnIsBusyChanged(bool value)
        {
            UnlockCommand.NotifyCanExecuteChanged();
            UnlockWithBiometricCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
            OnPropertyChanged(nameof(UnlockButtonText));
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));

        partial void OnIsPasswordHiddenChanged(bool value) => OnPropertyChanged(nameof(EyeGlyph));
    }
}
