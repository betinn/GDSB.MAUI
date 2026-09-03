namespace GDSB.Domain.Entities
{
    // Count precisa ser o primeiro valor: um JSON antigo sem a chave "BackupRetentionMode"
    // desserializa no default de enum (0), que precisa continuar sendo o comportamento por
    // quantidade, coerente com VaultSettings.BackupRetentionCount = 10.
    public enum BackupRetentionMode
    {
        Count,
        Days,
    }

    // Política de retenção de backups Rolling de um cofre, lida a partir de VaultSettings no
    // momento do Save (ver ProfileFileService.BackupBeforeOverwrite). LegacyV1 nunca é podado e
    // não é afetado por esta política (ver FileSystemVaultBackupStore).
    public record BackupRetentionPolicy(BackupRetentionMode Mode, int Count, int Days)
    {
        // Teto rígido, válido nos dois modos - impede o modo por dias de crescer sem limite.
        public const int HardCeiling = 100;

        public static BackupRetentionPolicy From(VaultSettings settings) =>
            new(settings.BackupRetentionMode, settings.BackupRetentionCount, settings.BackupRetentionDays);
    }
}
