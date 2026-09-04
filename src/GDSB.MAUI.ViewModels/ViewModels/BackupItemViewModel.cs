using System.Globalization;
using GDSB.Domain.Entities;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    // Envolve um VaultBackupInfo com propriedades já prontas pra UI (data local, tamanho legível,
    // rótulo do tipo), em vez de espalhar conversores pelo XAML - mesmo padrão de
    // SecretBoxItemViewModel. Não é ObservableObject: BackupRecoveryViewModel reconstrói a coleção
    // inteira no LanguageChanged (ver Refresh), mais barato que cada item assinar o evento.
    public class BackupItemViewModel
    {
        private readonly ILocalizationService _localization;

        public VaultBackupInfo Info { get; }

        public BackupItemViewModel(VaultBackupInfo info, ILocalizationService localization)
        {
            Info = info;
            _localization = localization;
        }

        public string VaultName => Info.VaultName;

        public string DisplayName => Info.DisplayName;

        // Formato e separador de data vêm do catálogo (Format_DateTimeShort); CultureInfo.CurrentCulture
        // explícito para não depender de a formatação de data respeitar a cultura da thread por
        // acaso.
        public string CreatedAtDisplay => Info.CreatedAtUtc.ToLocalTime()
            .ToString(_localization.Get("Format_DateTimeShort"), CultureInfo.CurrentCulture);

        // O separador decimal (",5" vs ".5") segue a cultura vigente; as unidades (B/KB/MB) também
        // vêm do catálogo, ainda que hoje o valor seja igual nos dois idiomas.
        public string SizeDisplay => Info.SizeBytes switch
        {
            < 1024 => $"{Info.SizeBytes.ToString(CultureInfo.CurrentCulture)} {_localization.Get("Format_SizeBytes")}",
            < 1024 * 1024 => $"{(Info.SizeBytes / 1024.0).ToString("0.#", CultureInfo.CurrentCulture)} {_localization.Get("Format_SizeKilobytes")}",
            _ => $"{(Info.SizeBytes / (1024.0 * 1024.0)).ToString("0.#", CultureInfo.CurrentCulture)} {_localization.Get("Format_SizeMegabytes")}",
        };

        public string KindLabel => _localization.Get(
            Info.Kind == VaultBackupKind.LegacyV1 ? "Backups_LegacyKindLabel" : "HelpVisual_SampleBackupKind");
    }
}
