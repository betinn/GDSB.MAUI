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
        // por aparelho, então guardar a última location (+ o nome do cofre, só pra exibição) já
        // basta. Preferences é usado direto aqui (sem uma interface própria) pelo mesmo motivo de
        // Launcher em VaultViewModel.OpenUrlAsync: é um detalhe de sessão, não algo que precisa de
        // mock nos fluxos já cobertos por teste.
        private const string LastLocationPreferenceKey = "gdsb.lastVaultLocation";
        private const string LastVaultNamePreferenceKey = "gdsb.lastVaultName";
        private const string BiometricPromptedPreferenceKey = "gdsb.biometricPrompted";

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IAlertService _alertService;
        private readonly IBiometricUnlockService _biometricUnlockService;

        // Resolvida quando o usuário responde ao overlay de opt-in (Aceitar/Agora não) - ver
        // AcceptBiometricOptIn/DeclineBiometricOptIn.
        private TaskCompletionSource<bool>? _biometricOptInResponse;

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

        [ObservableProperty]
        private string biometricVaultName = string.Empty;

        [ObservableProperty]
        private string biometricVaultPath = string.Empty;

        [ObservableProperty]
        private bool isBiometricOptInVisible;

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
            var lastLocation = Preferences.Default.Get<string?>(LastLocationPreferenceKey, null);
            if (string.IsNullOrEmpty(lastLocation))
            {
                CanUseBiometric = false;
                return;
            }

            CanUseBiometric = await _biometricUnlockService.IsAvailableAsync()
                && await _biometricUnlockService.IsEnabledAsync();

            if (CanUseBiometric)
            {
                BiometricVaultName = Preferences.Default.Get(LastVaultNamePreferenceKey, string.Empty);
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
                Preferences.Default.Set(LastVaultNamePreferenceKey, result.Profile.Nome);

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
        // próprio atalho de biometria. O overlay (ver UnlockPage.xaml) substitui um DisplayAlert
        // nativo justamente pra poder usar as cores/estilos do app em vez do diálogo do sistema.
        private async Task MaybeOfferBiometricOptInAsync(string password)
        {
            if (Preferences.Default.Get(BiometricPromptedPreferenceKey, false))
                return;

            if (!await _biometricUnlockService.IsAvailableAsync() || await _biometricUnlockService.IsEnabledAsync())
                return;

            Preferences.Default.Set(BiometricPromptedPreferenceKey, true);

            _biometricOptInResponse = new TaskCompletionSource<bool>();
            IsBiometricOptInVisible = true;
            var accepted = await _biometricOptInResponse.Task;

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

        [RelayCommand]
        private void AcceptBiometricOptIn()
        {
            IsBiometricOptInVisible = false;
            _biometricOptInResponse?.TrySetResult(true);
        }

        [RelayCommand]
        private void DeclineBiometricOptIn()
        {
            IsBiometricOptInVisible = false;
            _biometricOptInResponse?.TrySetResult(false);
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

            Preferences.Default.Remove(LastLocationPreferenceKey);
            Preferences.Default.Remove(LastVaultNamePreferenceKey);
            Preferences.Default.Remove(BiometricPromptedPreferenceKey);

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
