using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace GDSB.Infrastructure
{
    // Detecta o formato pelos 4 primeiros bytes do conteúdo (v2 começa com o magic "GDSB";
    // v1 é texto JSON, então nunca bate com o magic) e delega pro serviço certo. Save sempre
    // grava em v2. Toda leitura/gravação passa por IVaultFileSystem: esta classe nunca assume
    // que "location" é um caminho de arquivo de verdade (no Android pode ser um content:// URI).
    public class ProfileFileService(
        IFileDecryptionService legacyFileDecryptionService,
        IFileCryptoServiceV2 fileCryptoServiceV2,
        IVaultFileSystem fileSystem) : IProfileFileService
    {
        private static readonly byte[] V2Magic = { (byte)'G', (byte)'D', (byte)'S', (byte)'B' };

        public ProfileOpenResult Open(string location, string password)
        {
            var fileBytes = fileSystem.ReadAllBytes(location);

            if (IsV2Format(fileBytes))
            {
                var json = fileCryptoServiceV2.Decrypt(fileBytes, password);
                var profile = JsonSerializer.Deserialize<Profile>(json)
                    ?? throw new InvalidPasswordOrCorruptFileException();

                return new ProfileOpenResult(profile, WasLegacyFormat: false);
            }

            // Arquivos v1 sempre foram gravados como texto (File.WriteAllText, UTF-8 sem BOM) -
            // decodificar aqui em vez de repassar bytes mantém o leitor legado sem nenhuma
            // dependência de sistema de arquivos.
            var legacyContent = Encoding.UTF8.GetString(fileBytes);
            var legacyProfile = legacyFileDecryptionService.GetProfileDecrypted(legacyContent, password);
            return new ProfileOpenResult(legacyProfile, WasLegacyFormat: true);
        }

        public void Save(string location, Profile profile, string password)
        {
            BackupBeforeOverwrite(location);

            var json = JsonSerializer.Serialize(profile);
            var fileBytes = fileCryptoServiceV2.Encrypt(json, password);
            fileSystem.WriteAllBytes(location, fileBytes);
        }

        private void BackupBeforeOverwrite(string location)
        {
            if (!fileSystem.Exists(location))
                return;

            var currentBytes = fileSystem.ReadAllBytes(location);

            // O picker de salvar (FileSavePicker no Windows, ActionCreateDocument/SAF no Android)
            // já cria o arquivo de destino vazio antes do primeiro Save - isso não é um cofre v1
            // a preservar, é só o arquivo novo reservando o nome. Sem esse corte, todo cofre criado
            // do zero ganhava um ".v1.bak" vazio e desnecessário.
            if (currentBytes.Length == 0)
                return;

            if (IsV2Format(currentBytes))
            {
                // Backup "corrente": sempre a versão de antes do último save, sobrescrito a cada vez.
                var backupLocation = fileSystem.GetBackupLocation(location, ".bak");
                fileSystem.WriteAllBytes(backupLocation, currentBytes);
                return;
            }

            // Backup da migração v1 -> v2: preserva o original importado, nunca sobrescrito depois.
            var legacyBackupLocation = fileSystem.GetBackupLocation(location, ".v1.bak");
            if (!fileSystem.Exists(legacyBackupLocation))
                fileSystem.WriteAllBytes(legacyBackupLocation, currentBytes);
        }

        private static bool IsV2Format(byte[] fileBytes) =>
            fileBytes.Length >= V2Magic.Length && fileBytes.AsSpan(0, V2Magic.Length).SequenceEqual(V2Magic);
    }
}
