using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;

namespace GDSB.Infrastructure.Backup
{
    // Store único de backups nas duas plataformas: root é sempre um diretório privado do app
    // (FileSystem.AppDataDirectory/vault-backups, ver MauiProgram) - nunca mais "ao lado" do
    // cofre, nem no Windows. O hash da origin vira o nome da pasta, o que mantém a estabilidade
    // por cofre que o Android já tinha antes (dois cofres de mesmo nome não colidem); o nome
    // legível (BuildName) fica só no arquivo em si e no meta.json, que é o que a tela de
    // recuperação (fase 5) mostra.
    public class FileSystemVaultBackupStore(string root) : IVaultBackupStore
    {
        private const string MetaFileName = "meta.json";

        public VaultBackupInfo Store(string originLocation, string vaultName, byte[] previousBytes, VaultBackupKind kind)
        {
            var folder = FolderFor(originLocation);
            Directory.CreateDirectory(folder);

            var suffix = kind == VaultBackupKind.LegacyV1 ? VaultBackupNaming.LegacySuffix : VaultBackupNaming.RollingSuffix;
            var fileName = VaultBackupNaming.BuildName($"{vaultName}.GDSBX", suffix);
            var filePath = Path.Combine(folder, fileName);

            var meta = ReadMeta(folder) ?? new FolderMeta(originLocation, new Dictionary<string, EntryMeta>());

            // LegacyV1 preserva o original importado - nunca sobrescreve um já existente. Rolling
            // sempre sobrescreve (é sempre "a versão de antes do último save").
            if (kind == VaultBackupKind.LegacyV1
                && File.Exists(filePath)
                && meta.Entries.TryGetValue(fileName, out var existingEntry))
            {
                return new VaultBackupInfo(
                    filePath, fileName, existingEntry.VaultName, meta.OriginLocation,
                    existingEntry.Kind, existingEntry.CreatedAtUtc, new FileInfo(filePath).Length);
            }

            File.WriteAllBytes(filePath, previousBytes);

            var createdAtUtc = DateTime.UtcNow;
            meta = meta with { OriginLocation = originLocation };
            meta.Entries[fileName] = new EntryMeta(vaultName, kind, createdAtUtc);
            WriteMeta(folder, meta);

            return new VaultBackupInfo(filePath, fileName, vaultName, originLocation, kind, createdAtUtc, previousBytes.LongLength);
        }

        public IReadOnlyList<VaultBackupInfo> List()
        {
            var results = new List<VaultBackupInfo>();

            if (!Directory.Exists(root))
                return results;

            foreach (var folder in Directory.GetDirectories(root))
            {
                var meta = ReadMeta(folder);
                if (meta is null)
                    continue;

                foreach (var (fileName, entry) in meta.Entries)
                {
                    var filePath = Path.Combine(folder, fileName);
                    if (!File.Exists(filePath))
                        continue;

                    results.Add(new VaultBackupInfo(
                        filePath,
                        fileName,
                        entry.VaultName,
                        meta.OriginLocation,
                        entry.Kind,
                        entry.CreatedAtUtc,
                        new FileInfo(filePath).Length));
                }
            }

            return results;
        }

        public byte[] Read(VaultBackupInfo info) => File.ReadAllBytes(info.Id);

        public void Delete(VaultBackupInfo info)
        {
            if (File.Exists(info.Id))
                File.Delete(info.Id);

            var folder = Path.GetDirectoryName(info.Id);
            if (folder is null)
                return;

            var meta = ReadMeta(folder);
            if (meta is null)
                return;

            meta.Entries.Remove(Path.GetFileName(info.Id));
            RemoveFolderIfEmpty(folder, meta);
        }

        public void DeleteAllFor(string originLocation)
        {
            var folder = FolderFor(originLocation);
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }

        private string FolderFor(string originLocation)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(originLocation)));
            return Path.Combine(root, hash);
        }

        private static void RemoveFolderIfEmpty(string folder, FolderMeta meta)
        {
            if (meta.Entries.Count == 0)
            {
                Directory.Delete(folder, recursive: true);
                return;
            }

            WriteMeta(folder, meta);
        }

        private static FolderMeta? ReadMeta(string folder)
        {
            var metaPath = Path.Combine(folder, MetaFileName);
            if (!File.Exists(metaPath))
                return null;

            return JsonSerializer.Deserialize<FolderMeta>(File.ReadAllText(metaPath));
        }

        private static void WriteMeta(string folder, FolderMeta meta) =>
            File.WriteAllText(Path.Combine(folder, MetaFileName), JsonSerializer.Serialize(meta));

        private sealed record FolderMeta(string OriginLocation, Dictionary<string, EntryMeta> Entries);

        private sealed record EntryMeta(string VaultName, VaultBackupKind Kind, DateTime CreatedAtUtc);
    }
}
