using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using System.Security.Cryptography;
using System.Text;

// VaultPage vive no projeto GDSB.MAUI (host da UI) - este projeto não o referencia (senão viraria
// uma dependência circular), então a rota do Shell é passada como string literal, não nameof(...).
// Precisa continuar batendo com o nome registrado em AppShell.xaml.cs.
namespace GDSB.MAUI.ViewModels
{
    public partial class VaultSettingsViewModel : VaultProtectionsFormViewModelBase, IQueryAttributable
    {
        private const int MinPasswordLength = 8;

        // Deixam de ser const porque passam a vir do catálogo (ILocalizationService), que resolve
        // na cultura vigente a cada leitura. Nomes de propósito sem "password": a regra S2068 do
        // Sonar flaga qualquer identificador nesses moldes associado a um valor literal, mesmo
        // sendo só uma mensagem de UI.
        private string GenericSaveErrorMessage => Localization.Get("Vault_GenericSaveErrorMessage");

        private string WrongCurrentCodeMessage => Localization.Get("VaultSettings_WrongCurrentPasswordMessage");

        private string CodesDoNotMatchMessage => Localization.Get("Vault_PasswordsDoNotMatchMessage");

        private string BiometricReseloFailedMessage => Localization.Get("VaultSettings_BiometricResealFailedMessage");

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IBiometricUnlockService _biometricUnlockService;
        private readonly IVaultSessionService _vaultSessionService;
        private readonly IVaultBackupStore _backupStore;
        private readonly IAlertService _alertService;

        private Profile? _profile;
        private string? _location;
        private string? _password;

        public VaultSettingsViewModel(
            VaultAccess vaultAccess,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IBiometricUnlockService biometricUnlockService,
            IVaultBackupStore backupStore,
            IAlertService alertService,
            ILocalizationService localizationService,
            BiometricOptInCoordinator biometricOptIn)
            : base(localizationService)
        {
            _profileFileService = vaultAccess.ProfileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _biometricUnlockService = biometricUnlockService;
            _vaultSessionService = vaultAccess.VaultSessionService;
            _backupStore = backupStore;
            _alertService = alertService;
            BiometricOptIn = biometricOptIn;
        }

        public BiometricOptInCoordinator BiometricOptIn { get; }

        /// <summary>
        /// Disparado só quando nome, proteções ou senha são gravados com sucesso - mesmo selo de
        /// confirmação (modo Update) da VaultPage, nunca numa gravação que falhou.
        /// </summary>
        public event EventHandler? SettingsSaved;

        [ObservableProperty]
        private string vaultName = string.Empty;

        [ObservableProperty]
        private string? nameErrorMessage;

        [ObservableProperty]
        private string currentPassword = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmNewPassword = string.Empty;

        [ObservableProperty]
        private bool deleteOldBackups = true;

        [ObservableProperty]
        private string? passwordErrorMessage;

        // Aparece só depois de um save bem-sucedido que mexeu no ponto de desbloqueio (nome ou
        // senha) - nunca por causa das proteções, que gravam no arquivo atual sem prompt nenhum.
        [ObservableProperty]
        private bool showSaveAsNewFileOffer;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        // S2325 ("make static") é falso positivo aqui: essas quatro propriedades leem estado por
        // instância (via as propriedades geradas pelo [ObservableProperty] acima) e não podem virar
        // static sem quebrar o binding do XAML. O mesmo padrão já existe, sem supressão, em todos
        // os outros ViewModels (CanInteract => !IsBusy, etc.) - só não é reportado ali porque é
        // código antigo, fora da janela de "New Code" do Sonar.
#pragma warning disable S2325
        public bool HasNameErrorMessage => !string.IsNullOrEmpty(NameErrorMessage);

        public bool HasPasswordErrorMessage => !string.IsNullOrEmpty(PasswordErrorMessage);

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;
#pragma warning restore S2325

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Profile", out var profileValue) && profileValue is Profile profile)
            {
                _profile = profile;
                VaultName = profile.Nome;
                ClipboardClearEnabled = profile.Settings.ClipboardClearEnabled;
                ClipboardClearSeconds = profile.Settings.ClipboardClearSeconds;
                AutoLockEnabled = profile.Settings.AutoLockEnabled;
                AutoLockMinutes = profile.Settings.AutoLockMinutes;
                BackupRetentionMode = profile.Settings.BackupRetentionMode;
                BackupRetentionCount = profile.Settings.BackupRetentionCount;
                BackupRetentionDays = profile.Settings.BackupRetentionDays;
            }

            if (query.TryGetValue("Location", out var locationValue) && locationValue is string location)
                _location = location;

            if (query.TryGetValue("Password", out var passwordValue) && passwordValue is string password)
                _password = password;
        }

        [RelayCommand]
        private Task GoBackAsync() => _navigationService.GoBackAsync();

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task SaveNameAsync()
        {
            NameErrorMessage = null;

            if (_profile is null || _location is null || _password is null)
                return;

            if (string.IsNullOrWhiteSpace(VaultName))
            {
                NameErrorMessage = Localization.Get("Vault_NameRequiredMessage");
                return;
            }

            var newName = VaultName.Trim();

            IsBusy = true;
            try
            {
                _profile.Nome = newName;
                await Task.Run(() => _profileFileService.Save(_location, _profile, _password));

                BiometricOptIn.RememberVault(_location, newName);
                SettingsSaved?.Invoke(this, EventArgs.Empty);
                ShowSaveAsNewFileOffer = true;
            }
            catch (Exception)
            {
                NameErrorMessage = GenericSaveErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task SaveProtectionsAsync()
        {
            if (_profile is null || _location is null || _password is null)
                return;

            IsBusy = true;
            try
            {
                _profile.Settings = new VaultSettings
                {
                    ClipboardClearEnabled = ClipboardClearEnabled,
                    ClipboardClearSeconds = ClipboardClearSeconds,
                    AutoLockEnabled = AutoLockEnabled,
                    AutoLockMinutes = AutoLockMinutes,
                    BackupRetentionMode = BackupRetentionMode,
                    BackupRetentionCount = BackupRetentionCount,
                    BackupRetentionDays = BackupRetentionDays,
                };

                await Task.Run(() => _profileFileService.Save(_location, _profile, _password));
                _vaultSessionService.Start(_profile.Settings);
                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                ErrorMessage = GenericSaveErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task ChangePasswordAsync()
        {
            PasswordErrorMessage = null;

            if (_profile is null || _location is null || _password is null)
                return;

            if (NewPassword.Length < MinPasswordLength)
            {
                PasswordErrorMessage = Localization.Format("VaultSettings_MinNewPasswordLengthMessage", MinPasswordLength);
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                PasswordErrorMessage = CodesDoNotMatchMessage;
                return;
            }

            IsBusy = true;
            try
            {
                // Nunca confiar só na senha em memória - reabre o arquivo com a senha atual
                // informada pelo usuário.
                try
                {
                    await Task.Run(() => _profileFileService.Open(_location, CurrentPassword));
                }
                catch (Exception)
                {
                    PasswordErrorMessage = WrongCurrentCodeMessage;
                    return;
                }

                var enteredNewPassword = NewPassword;
                await Task.Run(() => _profileFileService.Save(_location, _profile, enteredNewPassword));

                if (DeleteOldBackups)
                    _backupStore.DeleteAllFor(_location);

                if (await _biometricUnlockService.IsEnabledAsync())
                {
                    await _biometricUnlockService.DisableAsync();

                    var secret = Encoding.UTF8.GetBytes(enteredNewPassword);
                    try
                    {
                        var reselado = await _biometricUnlockService.StoreKeyAsync(secret);
                        if (!reselado)
                            await _alertService.DisplayAlertAsync(null, BiometricReseloFailedMessage, Localization.Get("Common_Ok"));
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(secret);
                    }
                }

                _password = enteredNewPassword;
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;

                SettingsSaved?.Invoke(this, EventArgs.Empty);
                ShowSaveAsNewFileOffer = true;
            }
            catch (Exception)
            {
                PasswordErrorMessage = GenericSaveErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AcceptSaveAsNewFileAsync()
        {
            if (_profile is null || _password is null)
                return;

            string? location;
            try
            {
                location = await _filePickerService.PickSaveLocationAsync($"{_profile.Nome}.GDSBX");
            }
            catch (Exception)
            {
                ErrorMessage = Localization.Get("Vault_ChoosePathErrorMessage");
                return;
            }

            if (string.IsNullOrEmpty(location))
                return;

            IsBusy = true;
            try
            {
                await Task.Run(() => _profileFileService.Save(location, _profile, _password));

                // A partir daqui existem dois arquivos válidos com o mesmo conteúdo (o original
                // e o novo) - continuar editando nesta tela gravaria no arquivo errado sem o
                // usuário perceber (foi exatamente o bug relatado: renomear pro "arquivo B" e
                // toda mudança seguinte ainda ia pro "arquivo A"). Em vez de adivinhar qual dos
                // dois o usuário quer usar, encerra a sessão - a biometria mira um único arquivo
                // por vez, então também não faz sentido mantê-la selada aqui - e volta pra home:
                // o próximo desbloqueio escolhe o arquivo (A ou B) de forma explícita.
                try
                {
                    await _biometricUnlockService.DisableAsync();
                }
                catch (Exception)
                {
                    // Sem ação a tomar aqui: a sessão vai ser encerrada de qualquer jeito logo
                    // abaixo (ForgetVault + GoHomeAsync), então o atalho de biometria some do
                    // Preferences mesmo que DisableAsync falhe em limpar o lado da plataforma.
                }

                BiometricOptIn.ForgetVault();
                _vaultSessionService.Clear();
                ShowSaveAsNewFileOffer = false;

                await _navigationService.GoHomeAsync();
            }
            catch (Exception)
            {
                ErrorMessage = Localization.Get("Vault_SaveToLocationErrorMessage");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task DeclineSaveAsNewFileAsync()
        {
            ShowSaveAsNewFileOffer = false;
            return ReturnToVaultAsync(_location);
        }

        private Task ReturnToVaultAsync(string? location)
        {
            if (_profile is null || location is null || _password is null)
                return _navigationService.GoHomeAsync();

            return _navigationService.NavigateToRootAsync("VaultPage", new Dictionary<string, object>
            {
                ["Profile"] = _profile,
                ["Location"] = location,
                ["Password"] = _password,
            });
        }

        partial void OnIsBusyChanged(bool value)
        {
            SaveNameCommand.NotifyCanExecuteChanged();
            SaveProtectionsCommand.NotifyCanExecuteChanged();
            ChangePasswordCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
        }

        partial void OnNameErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasNameErrorMessage));

        partial void OnPasswordErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasPasswordErrorMessage));

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));
    }
}
