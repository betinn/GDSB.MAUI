using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Interfaces;
using GDSB.Infrastructure.Backup;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using System.Security.Cryptography;
using System.Text;

// UnlockPage/VaultPage/CreateVaultPage vivem no projeto GDSB.MAUI (host da UI) - este projeto não
// os referencia (senão viraria uma dependência circular), então as rotas do Shell são passadas
// como string literal, não nameof(...). Precisam continuar batendo com os nomes registrados em
// AppShell.xaml.cs.
namespace GDSB.MAUI.ViewModels
{
    public partial class UnlockViewModel : ObservableObject
    {
        // Senha errada e arquivo corrompido devem ser indistinguíveis pra quem usa o app -
        // nunca mostrar ex.Message cru, sempre essa mensagem genérica.
        private const string GenericErrorMessage = "Senha incorreta ou arquivo corrompido.";
        private const string EmptyPasswordMessage = "Digite a senha mestra do cofre.";
        private const string FilePickerErrorMessage = "Não foi possível abrir o seletor de arquivos.";
        private const string BackupFileAlertTitle = "Isto é um backup";
        private const string BackupFileAlertMessage =
            "Este arquivo parece ser um backup gerado automaticamente pelo GDSB. Você ainda pode " +
            "abri-lo normalmente com a senha mestra do cofre.";
        private const string BackupFileAlertCancel = "Entendi";

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IBiometricUnlockService _biometricUnlockService;
        private readonly IPreferencesService _preferencesService;
        private readonly IAlertService _alertService;
        private readonly IVaultSessionService _vaultSessionService;

        public UnlockViewModel(
            VaultAccess vaultAccess,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IBiometricUnlockService biometricUnlockService,
            IPreferencesService preferencesService,
            IAlertService alertService,
            BiometricOptInCoordinator biometricOptIn,
            OnboardingViewModel onboarding)
        {
            _profileFileService = vaultAccess.ProfileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _biometricUnlockService = biometricUnlockService;
            _preferencesService = preferencesService;
            _alertService = alertService;
            _vaultSessionService = vaultAccess.VaultSessionService;
            BiometricOptIn = biometricOptIn;
            Onboarding = onboarding;
        }

        // Exposto pra UnlockPage.xaml hospedar a BiometricOptInView (BindingContext="{Binding
        // BiometricOptIn}") - ver GDSB.MAUI.ViewModels.BiometricOptInCoordinator.
        public BiometricOptInCoordinator BiometricOptIn { get; }

        // Exposto pra UnlockPage.xaml hospedar a OnboardingView (BindingContext="{Binding
        // Onboarding}"), do mesmo jeito que a BiometricOptInView.
        public OnboardingViewModel Onboarding { get; }

        [ObservableProperty]
        private string? selectedVaultLocation;

        [ObservableProperty]
        private string? selectedVaultFileName;

        // Sonar não reconhece leitura/escrita de propriedade gerada por [ObservableProperty] como
        // "dado de instância" e sugere static - tornar estático quebraria o binding de XAML.
#pragma warning disable S2325
        public bool HasSelectedVault => !string.IsNullOrEmpty(SelectedVaultLocation);

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

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;
#pragma warning restore S2325

        // Com biometria ativa, o campo de senha some e a única forma de abrir um cofre é o mesmo
        // que ela mira (ou trocar de cofre, que a desativa) - ver ChangeVaultAsync: sem isso, dava
        // pra abrir manualmente um cofre B com uma biometria ainda selada com a senha do cofre A,
        // e a próxima tentativa por biometria tentava abrir B com a senha de A.
        // Idem justificativa de S2325 acima.
#pragma warning disable S2325
        public bool ShowManualUnlock => !CanUseBiometric;

        public string UnlockButtonText => IsBusy ? "Abrindo..." : "Abrir cofre";

        public string EyeGlyph => IsPasswordHidden ? "👁" : "🙈";
#pragma warning restore S2325

        [RelayCommand]
        private void ToggleShowPassword() => IsPasswordHidden = !IsPasswordHidden;

        // O link "Como funciona?" no topo da tela. Reabre o tutorial a qualquer momento, sem olhar
        // a preferência de "já vi": quem pediu pra rever quer rever.
        [RelayCommand]
        private void ShowOnboarding() => Onboarding.ShowFromStart();

        [RelayCommand]
        private Task GoToCreateVaultAsync() => _navigationService.NavigateToAsync("CreateVaultPage");

        [RelayCommand]
        private Task GoToBackupRecoveryAsync() => _navigationService.NavigateToAsync("BackupRecoveryPage");

        // Idem justificativa de S2325 acima - escreve em propriedades geradas por
        // [ObservableProperty], que o Sonar não reconhece como estado de instância.
#pragma warning disable S2325
        public void ClearPassword()
        {
            Password = string.Empty;
            ErrorMessage = null;
            IsPasswordHidden = true;
            SelectedVaultLocation = null;
            SelectedVaultFileName = null;
        }
#pragma warning restore S2325

        // "Trocar arquivo": some do lugar do nome escolhido e volta ao estado inicial - mesmo
        // efeito de reabrir a UnlockPage do zero.
        [RelayCommand]
        private void ClearSelectedVault() => ClearPassword();

        [RelayCommand]
        private async Task PickVaultAsync()
        {
            ErrorMessage = null;

            PickedFile? picked;
            try
            {
                picked = await _filePickerService.PickFileNameAsync();
            }
            catch (Exception)
            {
                ErrorMessage = FilePickerErrorMessage;
                return;
            }

            if (picked is null)
                return;

            SelectedVaultLocation = picked.Location;
            SelectedVaultFileName = picked.DisplayName;

            // Um backup também é um cofre válido - o aviso é só informativo, nunca bloqueia.
            if (VaultBackupNaming.IsBackupName(picked.DisplayName))
                await _alertService.DisplayAlertAsync(BackupFileAlertTitle, BackupFileAlertMessage, BackupFileAlertCancel);
        }

        // Chamado sempre que a UnlockPage passa a ser a tela mostrada - no OnAppearing (abrir o
        // app, voltar de outra tela) e também a cada Window.Resumed (o app volta de background,
        // com ou sem a página ter saído de "appeared" nesse meio-tempo). Atualiza se o atalho de
        // biometria deve aparecer e, se sim, já dispara o desbloqueio sozinho - o usuário não
        // precisa tocar em nada. O botão "Desbloquear com biometria" continua visível mesmo assim
        // (ver UnlockPage.xaml), pra quando o usuário cancelar o prompt do sistema sem querer (ou
        // ele falhar por qualquer motivo) e precisar tentar de novo manualmente. O guard CanUnlock()
        // evita disparar de novo por cima de uma tentativa já em andamento (ex.: OnAppearing e um
        // Window.Resumed quase simultâneo na abertura do app).
        public async Task InitializeAsync()
        {
            await RefreshBiometricAvailabilityAsync();

            // O tutorial não pode brigar com a biometria: com ela armada, este mesmo método já
            // dispara o prompt do sistema logo abaixo, e os dois por cima um do outro deixariam o
            // usuário sem saber em qual responder. No primeiro acesso de verdade isso nunca
            // acontece (não há cofre lembrado em Preferences), mas a guarda precisa existir para
            // quem já usa o app e ainda não viu os slides.
            if (!CanUseBiometric)
            {
                Onboarding.MaybeShowOnFirstRun();
                return;
            }

            if (CanUnlockWithBiometric())
                await UnlockWithBiometricAsync();
        }

        // Só reavalia o estado (disponível/habilitado + nome do cofre-alvo) - não dispara
        // biometria sozinha. Usado tanto pelo InitializeAsync quanto depois de uma tentativa que
        // falhou, pra não entrar em loop disparando o sensor de novo sozinha.
        private async Task RefreshBiometricAvailabilityAsync()
        {
            var lastLocation = _preferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null);
            if (string.IsNullOrEmpty(lastLocation))
            {
                CanUseBiometric = false;
                return;
            }

            CanUseBiometric = await _biometricUnlockService.IsAvailableAsync()
                && await _biometricUnlockService.IsEnabledAsync();

            if (CanUseBiometric)
                BiometricVaultName = _preferencesService.GetString(BiometricOptInCoordinator.LastVaultNamePreferenceKey, string.Empty) ?? string.Empty;
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

            if (SelectedVaultLocation is not { } selectedLocation)
                return;

            await OpenAndNavigateAsync(selectedLocation, Password, offerBiometricOptIn: true);
        }

        [RelayCommand(CanExecute = nameof(CanUnlockWithBiometric))]
        private async Task UnlockWithBiometricAsync()
        {
            var lastLocation = _preferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null);
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

                _vaultSessionService.Start(result.Profile.Settings);
                BiometricOptIn.RememberVault(location, result.Profile.Nome);

                if (offerBiometricOptIn)
                    await BiometricOptIn.MaybeOfferAsync(enteredPassword);

                ClearPassword();

                await _navigationService.NavigateToRootAsync("VaultPage", new Dictionary<string, object>
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
                // Intencional: mesmo se não houver atalho pra desativar (ou o Keystore falhar), a
                // troca de cofre precisa seguir em frente - ForgetVault abaixo já limpa o estado
                // relevante de qualquer forma.
            }

            BiometricOptIn.ForgetVault();

            CanUseBiometric = false;
            BiometricVaultName = string.Empty;
        }

        // UnlockCommand exige um arquivo escolhido; UnlockWithBiometricCommand não - a biometria
        // mira sempre o cofre lembrado em Preferences (LastLocationPreferenceKey), sem depender de
        // nenhuma seleção manual feita nesta tela.
        // Idem justificativa de S2325 acima - CanExecute de [RelayCommand], não pode virar static
        // sem quebrar o CommunityToolkit.Mvvm.
#pragma warning disable S2325
        private bool CanUnlock() => !IsBusy && HasSelectedVault;

        private bool CanUnlockWithBiometric() => !IsBusy;
#pragma warning restore S2325

        partial void OnIsBusyChanged(bool value)
        {
            UnlockCommand.NotifyCanExecuteChanged();
            UnlockWithBiometricCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
            OnPropertyChanged(nameof(UnlockButtonText));
        }

        partial void OnSelectedVaultLocationChanged(string? value)
        {
            OnPropertyChanged(nameof(HasSelectedVault));
            UnlockCommand.NotifyCanExecuteChanged();
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));

        partial void OnIsPasswordHiddenChanged(bool value) => OnPropertyChanged(nameof(EyeGlyph));

        partial void OnCanUseBiometricChanged(bool value) => OnPropertyChanged(nameof(ShowManualUnlock));
    }
}
