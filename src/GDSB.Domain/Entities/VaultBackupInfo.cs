namespace GDSB.Domain.Entities
{
    public enum VaultBackupKind
    {
        // Backup "corrente": sobrescrito a cada Save, guarda a versão de antes da última gravação.
        Rolling,

        // Backup da migração v1 -> v2: preserva o original importado, nunca sobrescrito depois.
        LegacyV1,
    }

    // Id é o caminho do backup dentro do IVaultBackupStore - opaco pra quem consome (só serve pra
    // devolver pro store em Read/Delete), mas também já é um caminho de arquivo real, então
    // IProfileFileService.Open aceita direto, sem nenhuma API nova.
    public record VaultBackupInfo(
        string Id,
        string DisplayName,
        string VaultName,
        string OriginLocation,
        VaultBackupKind Kind,
        DateTime CreatedAtUtc,
        long SizeBytes);
}
