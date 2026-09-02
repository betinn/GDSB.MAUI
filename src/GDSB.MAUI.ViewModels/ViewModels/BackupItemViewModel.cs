using GDSB.Domain.Entities;

namespace GDSB.MAUI.ViewModels
{
    // Envolve um VaultBackupInfo com propriedades já prontas pra UI (data local, tamanho legível,
    // rótulo do tipo), em vez de espalhar conversores pelo XAML - mesmo padrão de
    // SecretBoxItemViewModel.
    public class BackupItemViewModel
    {
        public VaultBackupInfo Info { get; }

        public BackupItemViewModel(VaultBackupInfo info)
        {
            Info = info;
        }

        public string VaultName => Info.VaultName;

        public string DisplayName => Info.DisplayName;

        public string CreatedAtDisplay => Info.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        public string SizeDisplay => Info.SizeBytes switch
        {
            < 1024 => $"{Info.SizeBytes} B",
            < 1024 * 1024 => $"{Info.SizeBytes / 1024.0:0.#} KB",
            _ => $"{Info.SizeBytes / (1024.0 * 1024.0):0.#} MB",
        };

        public string KindLabel => Info.Kind == VaultBackupKind.LegacyV1 ? "Migrado do formato antigo" : "Automático";
    }
}
