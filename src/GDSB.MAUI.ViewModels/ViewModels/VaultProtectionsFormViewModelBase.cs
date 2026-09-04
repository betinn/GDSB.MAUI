using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    // Estado e comandos do bloco PROTEÇÕES (limpeza de área de transferência, bloqueio
    // automático) e do bloco BACKUPS (retenção) - compartilhados entre VaultSettingsViewModel
    // (edição de um cofre existente) e CreateVaultViewModel (criação), que espelham exatamente
    // os mesmos campos e comandos de seleção. SaveProtectionsAsync/CreateVaultAsync continuam em
    // cada ViewModel, porque os dois fazem coisas fundamentalmente diferentes com esses valores.
    // Herda de LocalizedObject (em vez de ObservableObject direto) porque os textos dos chips de
    // opção (CreateVault_.../Protections_.../Backups_...) vêm do XAML via {loc:Tr}, não daqui -
    // trocar a base cobre VaultSettingsViewModel e CreateVaultViewModel de uma vez só.
    public abstract partial class VaultProtectionsFormViewModelBase : LocalizedObject
    {
        protected VaultProtectionsFormViewModelBase(ILocalizationService localizationService)
            : base(localizationService)
        {
        }

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

        // S2325 ("make static") é falso positivo aqui: essas propriedades leem estado por
        // instância (via a propriedade gerada por [ObservableProperty] acima) e não podem virar
        // static sem quebrar o binding do XAML.
#pragma warning disable S2325
        public bool IsBackupRetentionByCount => BackupRetentionMode == BackupRetentionMode.Count;

        public bool IsBackupRetentionByDays => BackupRetentionMode == BackupRetentionMode.Days;
#pragma warning restore S2325

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

        partial void OnBackupRetentionModeChanged(BackupRetentionMode value)
        {
            OnPropertyChanged(nameof(IsBackupRetentionByCount));
            OnPropertyChanged(nameof(IsBackupRetentionByDays));
        }
    }
}
