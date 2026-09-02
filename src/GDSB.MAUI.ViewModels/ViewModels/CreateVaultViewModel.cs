using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;

// VaultPage vive no projeto GDSB.MAUI (host da UI) - este projeto não o referencia (senão viraria
// uma dependência circular), então a rota do Shell é passada como string literal, não nameof(...).
// Precisa continuar batendo com o nome registrado em AppShell.xaml.cs.
namespace GDSB.MAUI.ViewModels
{
    public partial class CreateVaultViewModel : ObservableObject
    {
        private const int MinPasswordLength = 8;

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IBiometricUnlockService _biometricUnlockService;
        private readonly IVaultSessionService _vaultSessionService;

        public CreateVaultViewModel(
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IBiometricUnlockService biometricUnlockService,
            IVaultSessionService vaultSessionService,
            BiometricOptInCoordinator biometricOptIn)
        {
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _biometricUnlockService = biometricUnlockService;
            _vaultSessionService = vaultSessionService;
            BiometricOptIn = biometricOptIn;
        }

        // Exposto pra CreateVaultPage.xaml hospedar a BiometricOptInView (BindingContext="{Binding
        // BiometricOptIn}") - ver GDSB.MAUI.ViewModels.BiometricOptInCoordinator.
        public BiometricOptInCoordinator BiometricOptIn { get; }

        [ObservableProperty]
        private string vaultName = "Meu Cofre";

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        public static IReadOnlyList<int> ClipboardClearSecondsOptions { get; } = new[] { 20, 45, 90 };

        public static IReadOnlyList<int> AutoLockMinutesOptions { get; } = new[] { 1, 2, 5, 15 };

        public static IReadOnlyList<int> BackupRetentionCountOptions { get; } = new[] { 5, 10, 20, 50 };

        public static IReadOnlyList<int> BackupRetentionDaysOptions { get; } = new[] { 3, 5, 15, 30 };

        [ObservableProperty]
        private bool clipboardClearEnabled = true;

        [ObservableProperty]
        private int clipboardClearSeconds = 20;

        [ObservableProperty]
        private bool autoLockEnabled = true;

        [ObservableProperty]
        private int autoLockMinutes = 2;

        [ObservableProperty]
        private BackupRetentionMode backupRetentionMode = BackupRetentionMode.Count;

        [ObservableProperty]
        private int backupRetentionCount = 10;

        [ObservableProperty]
        private int backupRetentionDays = 5;

        // Recebe string, não int: o CommandParameter do XAML sempre chega como string (o binding
        // não converte pro tipo do parâmetro do RelayCommand), e RelayCommand<int> lança
        // InvalidCastException ao tentar converter esse valor - o clique simplesmente não fazia
        // nada, sem erro visível.
        [RelayCommand]
        private void SelectClipboardClearSeconds(string seconds) => ClipboardClearSeconds = int.Parse(seconds);

        [RelayCommand]
        private void SelectAutoLockMinutes(string minutes) => AutoLockMinutes = int.Parse(minutes);

        [RelayCommand]
        private void SelectBackupRetentionModeCount() => BackupRetentionMode = BackupRetentionMode.Count;

        [RelayCommand]
        private void SelectBackupRetentionModeDays() => BackupRetentionMode = BackupRetentionMode.Days;

        [RelayCommand]
        private void SelectBackupRetentionCount(string count) => BackupRetentionCount = int.Parse(count);

        [RelayCommand]
        private void SelectBackupRetentionDays(string days) => BackupRetentionDays = int.Parse(days);

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        // Sonar não reconhece leitura de propriedade gerada por [ObservableProperty] como "dado de
        // instância" e sugere static - tornar estático quebraria o binding de XAML.
#pragma warning disable S2325
        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;

        public string CreateButtonText => IsBusy ? "Criando..." : "Criar cofre";

        public bool IsBackupRetentionByCount => BackupRetentionMode == BackupRetentionMode.Count;

        public bool IsBackupRetentionByDays => BackupRetentionMode == BackupRetentionMode.Days;
#pragma warning restore S2325

        [RelayCommand]
        private Task GoBackAsync() => _navigationService.GoBackAsync();

        [RelayCommand(CanExecute = nameof(CanCreate))]
        private async Task CreateVaultAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(VaultName))
            {
                ErrorMessage = "Dê um nome ao cofre.";
                return;
            }

            if (Password.Length < MinPasswordLength)
            {
                ErrorMessage = $"A senha mestra precisa ter pelo menos {MinPasswordLength} caracteres.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "As senhas não coincidem.";
                return;
            }

            string? location;
            try
            {
                location = await _filePickerService.PickSaveLocationAsync($"{VaultName}.GDSBX");
            }
            catch (Exception)
            {
                ErrorMessage = "Não foi possível escolher onde salvar o cofre.";
                return;
            }

            if (string.IsNullOrEmpty(location))
                return;

            IsBusy = true;
            try
            {
                var profile = new Profile
                {
                    Nome = VaultName.Trim(),
                    Settings = new VaultSettings
                    {
                        ClipboardClearEnabled = ClipboardClearEnabled,
                        ClipboardClearSeconds = ClipboardClearSeconds,
                        AutoLockEnabled = AutoLockEnabled,
                        AutoLockMinutes = AutoLockMinutes,
                        BackupRetentionMode = BackupRetentionMode,
                        BackupRetentionCount = BackupRetentionCount,
                        BackupRetentionDays = BackupRetentionDays,
                    },
                };
                var enteredPassword = Password;

                await Task.Run(() => _profileFileService.Save(location, profile, enteredPassword));
                _vaultSessionService.Start(profile.Settings);

                // Um cofre novo nunca deve herdar o atalho de biometria de outro que o usuário
                // tinha aberto antes - sem isso, criar o cofre B com a biometria do cofre A ainda
                // ativa deixava o atalho selado com a senha errada pra qualquer um dos dois. Some
                // com o antigo (se houver) e oferece de novo, já mirando o cofre recém-criado.
                try
                {
                    await _biometricUnlockService.DisableAsync();
                }
                catch (Exception)
                {
                    // Intencional: mesmo se não houver atalho de biometria pra desativar (ou o
                    // Keystore falhar), o cofre novo precisa ser criado do mesmo jeito - a oferta
                    // de biometria abaixo é o caminho normal pra religar, se o usuário quiser.
                }

                BiometricOptIn.ForgetVault();
                BiometricOptIn.RememberVault(location, profile.Nome);
                await BiometricOptIn.MaybeOfferAsync(enteredPassword);

                await _navigationService.NavigateToRootAsync("VaultPage", new Dictionary<string, object>
                {
                    ["Profile"] = profile,
                    ["Location"] = location,
                    ["Password"] = enteredPassword,
                });
            }
            catch (Exception)
            {
                ErrorMessage = "Não foi possível criar o cofre nesse local.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Idem: referencia IsBusy (gerado por [ObservableProperty]) e é o CanExecute do
        // [RelayCommand] acima - não pode virar static sem quebrar o CommunityToolkit.Mvvm.
#pragma warning disable S2325
        private bool CanCreate() => !IsBusy;
#pragma warning restore S2325

        partial void OnIsBusyChanged(bool value)
        {
            CreateVaultCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
            OnPropertyChanged(nameof(CreateButtonText));
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));

        partial void OnBackupRetentionModeChanged(BackupRetentionMode value)
        {
            OnPropertyChanged(nameof(IsBackupRetentionByCount));
            OnPropertyChanged(nameof(IsBackupRetentionByDays));
        }
    }
}
