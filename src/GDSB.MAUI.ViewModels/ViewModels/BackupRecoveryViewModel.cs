using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    // Tela de recuperação: lista os backups do IVaultBackupStore (sempre fora da pasta do cofre,
    // ver FileSystemVaultBackupStore), restaura um deles pra um arquivo novo escolhido pelo usuário
    // (nunca sobrescreve o cofre original) ou exclui backups, um por vez ou todos de uma vez.
    public partial class BackupRecoveryViewModel : LocalizedObject
    {
        // Deixam de ser const porque passam a vir do catálogo (ILocalizationService), que resolve
        // na cultura vigente a cada leitura. Nome de propósito sem "password": a regra S2068 do
        // Sonar ("Hard-coded credentials") flaga qualquer identificador nesses moldes atribuído a
        // um valor literal, mesmo sendo só uma mensagem de UI.
        private string GenericRestoreErrorMessage => Localization.Get("Unlock_GenericErrorMessage");

        private string EmptyCodeMessage => Localization.Get("BackupRecovery_EmptyPasswordMessage");

        private string FilePickerErrorMessage => Localization.Get("Vault_ChoosePathErrorMessage");

        private string GenericSaveErrorMessage => Localization.Get("Vault_SaveToLocationErrorMessage");

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
            IVaultSessionService vaultSessionService,
            ILocalizationService localizationService)
            : base(localizationService)
        {
            _backupStore = backupStore;
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
            _vaultSessionService = vaultSessionService;

            // LocalizedObject só reemite OnPropertyChanged("") na troca de idioma - suficiente para
            // propriedades calculadas, mas não para os itens já materializados em Backups: cada
            // BackupItemViewModel não é ObservableObject (ver comentário na classe), então a
            // CollectionView não percebe que CreatedAtDisplay/SizeDisplay/KindLabel mudaram sem a
            // coleção ser reconstruída. Assina LanguageChanged direto, além do que a base já faz.
            Localization.LanguageChanged += OnLanguageChangedRefresh;
        }

        private void OnLanguageChangedRefresh(object? sender, EventArgs e)
        {
            if (Backups.Count > 0)
                Refresh();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Localization.LanguageChanged -= OnLanguageChangedRefresh;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Disparado só quando um backup foi realmente apagado do IVaultBackupStore (nunca antes
        /// de confirmar) - a view usa isso pra tocar a mesma animação de "selo estilhaçado" da
        /// exclusão de item em VaultPage, deixando visível que aquele backup foi destruído.
        /// </summary>
        public event EventHandler? BackupDeleted;

        /// <summary>Disparado quando "excluir todos" apagou pelo menos um backup.</summary>
        public event EventHandler? AllBackupsDeleted;

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
                Backups.Add(new BackupItemViewModel(info, Localization));

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
                RestoreErrorMessage = EmptyCodeMessage;
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
            BackupDeleted?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void PromptDeleteAll() => IsConfirmingDeleteAll = true;

        [RelayCommand]
        private void CancelDeleteAll() => IsConfirmingDeleteAll = false;

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void ConfirmDeleteAll()
        {
            var deletedAny = Backups.Count > 0;

            foreach (var item in Backups.ToList())
                _backupStore.Delete(item.Info);

            IsConfirmingDeleteAll = false;
            Refresh();

            if (deletedAny)
                AllBackupsDeleted?.Invoke(this, EventArgs.Empty);
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
