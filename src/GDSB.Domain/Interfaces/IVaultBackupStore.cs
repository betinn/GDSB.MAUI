using GDSB.Domain.Entities;

namespace GDSB.Domain.Interfaces
{
    // Store único de backups, fora da pasta do cofre nas duas plataformas (ver
    // FileSystemVaultBackupStore, em GDSB.Infrastructure). ProfileFileService usa Store durante o
    // Save; a tela de recuperação (fase 5) usa List/Read/Delete/DeleteAllFor.
    public interface IVaultBackupStore
    {
        // Grava previousBytes (o conteúdo do cofre de antes da sobrescrita) como backup de
        // originLocation e devolve o VaultBackupInfo resultante. Rolling acumula uma versão por
        // save (nome com timestamp) e poda pelo retention logo em seguida; LegacyV1 nunca
        // sobrescreve um já existente e nunca é podado.
        VaultBackupInfo Store(string originLocation, string vaultName, byte[] previousBytes, VaultBackupKind kind, BackupRetentionPolicy retention);

        IReadOnlyList<VaultBackupInfo> List();

        byte[] Read(VaultBackupInfo info);

        void Delete(VaultBackupInfo info);

        void DeleteAllFor(string originLocation);
    }
}
