using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    // Tela de recuperação: lista os backups do IVaultBackupStore (sempre fora da pasta do cofre,
    // ver FileSystemVaultBackupStore), restaura um deles pra um arquivo novo escolhido pelo usuário
    // (nunca sobrescreve o cofre original) ou exclui backups, um por vez ou todos de uma vez.
    public partial class BackupRecoveryViewModel : ObservableObject
    {
        private const string GenericRestoreErrorMessage = "Senha incorreta ou arquivo corrompido.";
        private const string EmptyPasswordMessage = "Digite a senha mestra deste backup.";
        private const string FilePickerErrorMessage = "Não foi possível escolher onde salvar o cofre.";
        private const string GenericSaveErrorMessage = "Não foi possível salvar o cofre nesse local.";

        private readonly IVaultBackupStore _backupStore;
        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;
        private readonly IVaultSessionService _vaultSessionService;

        public BackupRecoveryViewModel(
            IVaultBackupStore backupStore,
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService,
            IVaultSessionService vaultSessionService)
        {
            _backupStore = backupStore;
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _vaultSessionService = vaultSessionService;
        }

        public ObservableCollection<BackupItemViewModel> Backups { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        // Item sendo restaurado ou excluído no momento - controla qual painel de confirmação
        // aparece na UI (só um por vez).
        [ObservableProperty]
        private BackupItemViewModel? pendingRestoreItem;

        [ObservableProperty]
        private BackupItemViewModel? pendingDeleteItem;

        [ObservableProperty]
        private bool isConfirmingDeleteAll;

        [ObservableProperty]
        private string restorePassword = string.Empty;

        [ObservableProperty]
        private string? restoreErrorMessage;

        // Bloco de propriedades computadas somente leitura, derivadas de propriedades geradas por
        // [ObservableProperty] - o Sonar não reconhece esse acesso como "dado de instância" e
        // sugere static (S2325); tornar qualquer uma delas estática quebraria o binding de XAML.
#pragma warning disable S2325
        public bool HasBackups => Backups.Count > 0;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool IsRestoring => PendingRestoreItem is not null;

        public bool IsConfirmingDelete => PendingDeleteItem is not null;

        public bool HasRestoreErrorMessage => !string.IsNullOrEmpty(RestoreErrorMessage);

        public bool CanInteract => !IsBusy;
#pragma warning restore S2325

        public void Initialize() => Refresh();

        private void Refresh()
        {
            Backups.Clear();
            foreach (var info in _backupStore.List().OrderByDescending(i => i.CreatedAtUtc))
                Backups.Add(new BackupItemViewModel(info));

            OnPropertyChanged(nameof(HasBackups));
        }

        [RelayCommand]
        private Task GoBackAsync() => _navigationService.GoBackAsync();

        [RelayCommand]
        private void BeginRestore(BackupItemViewModel item)
        {
            PendingRestoreItem = item;
            RestorePassword = string.Empty;
            RestoreErrorMessage = null;
        }

        [RelayCommand]
        private void CancelRestore()
        {
            PendingRestoreItem = null;
            RestorePassword = string.Empty;
            RestoreErrorMessage = null;
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task ConfirmRestoreAsync()
        {
            if (PendingRestoreItem is not { } item)
                return;

            RestoreErrorMessage = null;

            if (string.IsNullOrEmpty(RestorePassword))
            {
                RestoreErrorMessage = EmptyPasswordMessage;
                return;
            }

            var password = RestorePassword;

            IsBusy = true;
            try
            {
                ProfileOpenResult result;
                try
                {
                    result = await Task.Run(() => _profileFileService.Open(item.Info.Id, password));
                }
                catch (Exception)
                {
                    // Cobre tanto senha errada quanto um backup corrompido - a mensagem pro
                    // usuário é sempre a mesma, de propósito (mesma regra do UnlockViewModel).
                    RestoreErrorMessage = GenericRestoreErrorMessage;
                    return;
                }

                string? location;
                try
                {
                    location = await _filePickerService.PickSaveLocationAsync($"{item.VaultName}.GDSBX");
                }
                catch (Exception)
                {
                    RestoreErrorMessage = FilePickerErrorMessage;
                    return;
                }

                if (string.IsNullOrEmpty(location))
                    return;

                await Task.Run(() => _profileFileService.Save(location, result.Profile, password));
                _vaultSessionService.Start(result.Profile.Settings);

                PendingRestoreItem = null;
                RestorePassword = string.Empty;

                await _navigationService.NavigateToRootAsync("VaultPage", new Dictionary<string, object>
                {
                    ["Profile"] = result.Profile,
                    ["Location"] = location,
                    ["Password"] = password,
                });
            }
            catch (Exception)
            {
                RestoreErrorMessage = GenericSaveErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void PromptDelete(BackupItemViewModel item) => PendingDeleteItem = item;

        [RelayCommand]
        private void CancelDelete() => PendingDeleteItem = null;

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void ConfirmDelete()
        {
            if (PendingDeleteItem is not { } item)
                return;

            _backupStore.Delete(item.Info);
            PendingDeleteItem = null;
            Refresh();
        }

        [RelayCommand]
        private void PromptDeleteAll() => IsConfirmingDeleteAll = true;

        [RelayCommand]
        private void CancelDeleteAll() => IsConfirmingDeleteAll = false;

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void ConfirmDeleteAll()
        {
            foreach (var item in Backups.ToList())
                _backupStore.Delete(item.Info);

            IsConfirmingDeleteAll = false;
            Refresh();
        }

        partial void OnIsBusyChanged(bool value)
        {
            ConfirmRestoreCommand.NotifyCanExecuteChanged();
            ConfirmDeleteCommand.NotifyCanExecuteChanged();
            ConfirmDeleteAllCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));

        partial void OnRestoreErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasRestoreErrorMessage));

        partial void OnPendingRestoreItemChanged(BackupItemViewModel? value) => OnPropertyChanged(nameof(IsRestoring));

        partial void OnPendingDeleteItemChanged(BackupItemViewModel? value) => OnPropertyChanged(nameof(IsConfirmingDelete));
    }
}
