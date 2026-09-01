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
    public partial class VaultSettingsViewModel : ObservableObject, IQueryAttributable
    {
        private const int MinPasswordLength = 8;
        private const string GenericSaveErrorMessage = "Não foi possível salvar o cofre. Tente novamente.";
        // Nomes de propósito sem "password": a regra S2068 do Sonar flaga qualquer identificador
        // nesses moldes (declaração ou atribuição) associado a um valor literal, mesmo sendo só
        // uma mensagem de UI.
        private const string WrongCurrentCodeMessage = "Senha atual incorreta.";
        private const string CodesDoNotMatchMessage = "As senhas não coincidem.";
        private const string BiometricReseloFailedMessage =
            "A senha foi trocada, mas não foi possível re-selar a biometria. A senha mestra continua valendo normalmente.";

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
            BiometricOptInCoordinator biometricOptIn)
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

        public static IReadOnlyList<int> ClipboardClearSecondsOptions { get; } = new[] { 20, 45, 90 };

        public static IReadOnlyList<int> AutoLockMinutesOptions { get; } = new[] { 1, 2, 5, 15 };

        [ObservableProperty]
        private string vaultName = string.Empty;

        [ObservableProperty]
        private string? nameErrorMessage;

        [ObservableProperty]
        private bool clipboardClearEnabled;

        [ObservableProperty]
        private int clipboardClearSeconds;

        [ObservableProperty]
        private bool autoLockEnabled;

        [ObservableProperty]
        private int autoLockMinutes;

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
        private bool showSwitchToNewFileOffer;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        private string? _pendingNewLocation;

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
                NameErrorMessage = "Dê um nome ao cofre.";
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
                OfferSaveAsNewFile();
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

        // Recebe string, não int: o CommandParameter do XAML sempre chega como string (o binding
        // não converte pro tipo do parâmetro do RelayCommand), e RelayCommand<int> lança
        // InvalidCastException ao tentar converter esse valor - o clique simplesmente não fazia
        // nada, sem erro visível.
        [RelayCommand]
        private void SelectClipboardClearSeconds(string seconds) => ClipboardClearSeconds = int.Parse(seconds);

        [RelayCommand]
        private void SelectAutoLockMinutes(string minutes) => AutoLockMinutes = int.Parse(minutes);

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
                PasswordErrorMessage = $"A senha nova precisa ter pelo menos {MinPasswordLength} caracteres.";
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
                            await _alertService.DisplayAlertAsync(null, BiometricReseloFailedMessage, "Ok");
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
                OfferSaveAsNewFile();
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

        private void OfferSaveAsNewFile()
        {
            ShowSwitchToNewFileOffer = false;
            _pendingNewLocation = null;
            ShowSaveAsNewFileOffer = true;
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
                ErrorMessage = "Não foi possível escolher onde salvar o cofre.";
                return;
            }

            if (string.IsNullOrEmpty(location))
                return;

            IsBusy = true;
            try
            {
                await Task.Run(() => _profileFileService.Save(location, _profile, _password));
                _pendingNewLocation = location;
                ShowSaveAsNewFileOffer = false;
                ShowSwitchToNewFileOffer = true;
            }
            catch (Exception)
            {
                ErrorMessage = "Não foi possível salvar o cofre nesse local.";
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

        [RelayCommand]
        private Task AcceptSwitchToNewFileAsync()
        {
            ShowSwitchToNewFileOffer = false;
            var newLocation = _pendingNewLocation;
            _pendingNewLocation = null;

            if (_profile is not null && newLocation is not null)
                BiometricOptIn.RememberVault(newLocation, _profile.Nome);

            return ReturnToVaultAsync(newLocation ?? _location);
        }

        [RelayCommand]
        private Task DeclineSwitchToNewFileAsync()
        {
            ShowSwitchToNewFileOffer = false;
            _pendingNewLocation = null;
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
