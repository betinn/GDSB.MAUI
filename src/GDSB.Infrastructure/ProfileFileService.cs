using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.Domain.Interfaces;
using System.Text.Json;

namespace GDSB.Infrastructure
{
    // Detecta o formato pelos 4 primeiros bytes do arquivo (v2 começa com o magic "GDSB";
    // v1 é texto JSON, então nunca bate com o magic) e delega pro serviço certo. Save sempre
    // grava em v2 — se o arquivo de destino ainda estiver em v1, faz backup antes de sobrescrever.
    public class ProfileFileService(IFileDecryptionService legacyFileDecryptionService, IFileCryptoServiceV2 fileCryptoServiceV2) : IProfileFileService
    {
        private static readonly byte[] V2Magic = { (byte)'G', (byte)'D', (byte)'S', (byte)'B' };

        public ProfileOpenResult Open(string path, string password)
        {
            if (IsV2Format(path))
            {
                var fileBytes = File.ReadAllBytes(path);
                var json = fileCryptoServiceV2.Decrypt(fileBytes, password);
                var profile = JsonSerializer.Deserialize<Profile>(json)
                    ?? throw new InvalidPasswordOrCorruptFileException();

                return new ProfileOpenResult(profile, WasLegacyFormat: false);
            }

            var legacyProfile = legacyFileDecryptionService.GetProfileDecrypted(path, password);
            return new ProfileOpenResult(legacyProfile, WasLegacyFormat: true);
        }

        public void Save(string path, Profile profile, string password)
        {
            BackupIfLegacy(path);

            var json = JsonSerializer.Serialize(profile);
            var fileBytes = fileCryptoServiceV2.Encrypt(json, password);
            File.WriteAllBytes(path, fileBytes);
        }

        private static void BackupIfLegacy(string path)
        {
            if (!File.Exists(path) || IsV2Format(path))
                return;

            var backupPath = path + ".v1.bak";
            if (!File.Exists(backupPath))
                File.Copy(path, backupPath);
        }

        private static bool IsV2Format(string path)
        {
            if (!File.Exists(path))
                return false;

            using var stream = File.OpenRead(path);
            if (stream.Length < V2Magic.Length)
                return false;

            var buffer = new byte[V2Magic.Length];
            var read = stream.Read(buffer, 0, buffer.Length);
            return read == V2Magic.Length && buffer.AsSpan().SequenceEqual(V2Magic);
        }
    }
}
