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

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IBiometricUnlockService _biometricUnlockService;

        public UnlockViewModel(
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IBiometricUnlockService biometricUnlockService,
            BiometricOptInCoordinator biometricOptIn)
        {
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _biometricUnlockService = biometricUnlockService;
            BiometricOptIn = biometricOptIn;
        }

        // Exposto pra UnlockPage.xaml hospedar a BiometricOptInView (BindingContext="{Binding
        // BiometricOptIn}") - ver GDSB.MAUI.ViewModels.BiometricOptInCoordinator.
        public BiometricOptInCoordinator BiometricOptIn { get; }

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

        [ObservableProperty]
        private string biometricVaultName = string.Empty;

        [ObservableProperty]
        private string biometricVaultPath = string.Empty;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;

        // Com biometria ativa, o campo de senha some e a única forma de abrir um cofre é o mesmo
        // que ela mira (ou trocar de cofre, que a desativa) - ver ChangeVaultAsync: sem isso, dava
        // pra abrir manualmente um cofre B com uma biometria ainda selada com a senha do cofre A,
        // e a próxima tentativa por biometria tentava abrir B com a senha de A.
        public bool ShowManualUnlock => !CanUseBiometric;

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

        // Chamado no OnAppearing da UnlockPage: atualiza se o atalho de biometria deve aparecer e,
        // se sim, já dispara o desbloqueio sozinho - o usuário não precisa tocar em nada. O botão
        // "Desbloquear com biometria" continua visível mesmo assim (ver UnlockPage.xaml), pra quando
        // o usuário cancelar o prompt do sistema sem querer (ou ele falhar por qualquer motivo) e
        // precisar tentar de novo manualmente.
        public async Task InitializeAsync()
        {
            await RefreshBiometricAvailabilityAsync();

            if (CanUseBiometric)
                await UnlockWithBiometricAsync();
        }

        // Só reavalia o estado (disponível/habilitado + nome e path do cofre-alvo) - não dispara
        // biometria sozinha. Usado tanto pelo InitializeAsync quanto depois de uma tentativa que
        // falhou, pra não entrar em loop disparando o sensor de novo sozinha.
        private async Task RefreshBiometricAvailabilityAsync()
        {
            var lastLocation = Preferences.Default.Get<string?>(BiometricOptInCoordinator.LastLocationPreferenceKey, null);
            if (string.IsNullOrEmpty(lastLocation))
            {
                CanUseBiometric = false;
                return;
            }

            CanUseBiometric = await _biometricUnlockService.IsAvailableAsync()
                && await _biometricUnlockService.IsEnabledAsync();

            if (CanUseBiometric)
            {
                BiometricVaultName = Preferences.Default.Get(BiometricOptInCoordinator.LastVaultNamePreferenceKey, string.Empty);
                BiometricVaultPath = lastLocation;
            }
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
            var lastLocation = Preferences.Default.Get<string?>(BiometricOptInCoordinator.LastLocationPreferenceKey, null);
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

                BiometricOptInCoordinator.RememberVault(location, result.Profile.Nome);

                if (offerBiometricOptIn)
                    await BiometricOptIn.MaybeOfferAsync(enteredPassword);

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

        // "Trocar cofre": a biometria só faz sentido presa a um cofre por vez (é uma senha selada,
        // não uma chave universal), então trocar de cofre precisa apagar o atalho atual por
        // completo - incluindo o "já perguntei" - pra que o próximo Open bem-sucedido (de
        // qualquer cofre, o mesmo ou outro) ofereça o opt-in de novo, agora mirando o cofre certo.
        [RelayCommand]
        private async Task ChangeVaultAsync()
        {
            try
            {
                await _biometricUnlockService.DisableAsync();
            }
            catch (Exception)
            {
            }

            BiometricOptInCoordinator.ForgetVault();

            CanUseBiometric = false;
            BiometricVaultName = string.Empty;
            BiometricVaultPath = string.Empty;
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

        partial void OnCanUseBiometricChanged(bool value) => OnPropertyChanged(nameof(ShowManualUnlock));
    }
}
