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
    public class FileSystemVaultBackupStore(string root, Func<DateTime>? utcNow = null) : IVaultBackupStore
    {
        private const string MetaFileName = "meta.json";

        // Injetável só em teste - é o que torna a poda por dias testável sem depender do relógio
        // real da máquina.
        private readonly Func<DateTime> _utcNow = utcNow ?? (() => DateTime.UtcNow);

        public VaultBackupInfo Store(string originLocation, string vaultName, byte[] previousBytes, VaultBackupKind kind, BackupRetentionPolicy retention)
        {
            var folder = FolderFor(originLocation);
            Directory.CreateDirectory(folder);

            var meta = ReadMeta(folder) ?? new FolderMeta(originLocation, new Dictionary<string, EntryMeta>());
            meta = meta with { OriginLocation = originLocation };

            if (kind == VaultBackupKind.LegacyV1)
            {
                var fileName = VaultBackupNaming.BuildName($"{vaultName}.GDSBX", VaultBackupNaming.LegacySuffix);
                var filePath = Path.Combine(folder, fileName);

                // LegacyV1 preserva o original importado - nunca sobrescreve um já existente.
                if (File.Exists(filePath) && meta.Entries.TryGetValue(fileName, out var existingEntry))
                {
                    return new VaultBackupInfo(
                        filePath, fileName, existingEntry.VaultName, meta.OriginLocation,
                        existingEntry.Kind, existingEntry.CreatedAtUtc, new FileInfo(filePath).Length);
                }

                File.WriteAllBytes(filePath, previousBytes);

                var legacyCreatedAtUtc = _utcNow();
                meta.Entries[fileName] = new EntryMeta(vaultName, kind, legacyCreatedAtUtc);
                WriteMeta(folder, meta);

                return new VaultBackupInfo(filePath, fileName, vaultName, originLocation, kind, legacyCreatedAtUtc, previousBytes.LongLength);
            }

            // Rolling agora acumula uma versão por save, em vez de sobrescrever a anterior.
            var createdAtUtc = _utcNow();
            var (rollingFileName, rollingFilePath) = BuildUniqueRollingPath(folder, vaultName, createdAtUtc);

            File.WriteAllBytes(rollingFilePath, previousBytes);
            meta.Entries[rollingFileName] = new EntryMeta(vaultName, kind, createdAtUtc);

            var result = new VaultBackupInfo(rollingFilePath, rollingFileName, vaultName, originLocation, kind, createdAtUtc, previousBytes.LongLength);

            Prune(folder, meta, retention);

            return result;
        }

        // Dois saves no mesmo segundo geram o mesmo nome (timestamp com granularidade de
        // segundo) - sufixa "(2)", "(3)"... antes da extensão até achar um caminho livre.
        private static (string fileName, string filePath) BuildUniqueRollingPath(string folder, string vaultName, DateTime createdAtUtc)
        {
            var fileName = VaultBackupNaming.BuildName($"{vaultName}.GDSBX", VaultBackupNaming.RollingSuffix, createdAtUtc);
            var filePath = Path.Combine(folder, fileName);
            if (!File.Exists(filePath))
                return (fileName, filePath);

            var stem = fileName[..^VaultBackupNaming.RollingSuffix.Length];
            var attempt = 2;
            while (true)
            {
                var candidateName = $"{stem} ({attempt}){VaultBackupNaming.RollingSuffix}";
                var candidatePath = Path.Combine(folder, candidateName);
                if (!File.Exists(candidatePath))
                    return (candidateName, candidatePath);

                attempt++;
            }
        }

        // Regras: só entradas Rolling entram na conta; LegacyV1 nunca é podado. Nos dois modos
        // vale o teto rígido (HardCeiling) e o piso de nunca apagar a versão mais recente.
        // Aproveita a passada para limpar do meta.json entradas cujo arquivo já sumiu.
        private void Prune(string folder, FolderMeta meta, BackupRetentionPolicy retention)
        {
            foreach (var orphan in meta.Entries.Where(e => !File.Exists(Path.Combine(folder, e.Key))).Select(e => e.Key).ToList())
                meta.Entries.Remove(orphan);

            var rollingByRecency = meta.Entries
                .Where(e => e.Value.Kind == VaultBackupKind.Rolling)
                .OrderByDescending(e => e.Value.CreatedAtUtc)
                .ToList();

            var eligible = retention.Mode == BackupRetentionMode.Days
                ? rollingByRecency.Where(e => e.Value.CreatedAtUtc >= _utcNow() - TimeSpan.FromDays(retention.Days))
                : rollingByRecency.Take(Math.Max(retention.Count, 0));

            var toKeep = eligible.Take(BackupRetentionPolicy.HardCeiling).Select(e => e.Key).ToHashSet();

            // Piso: se a regra apagaria tudo, mantém pelo menos a versão mais recente.
            if (toKeep.Count == 0 && rollingByRecency.Count > 0)
                toKeep.Add(rollingByRecency[0].Key);

            foreach (var (fileName, _) in rollingByRecency.Where(e => !toKeep.Contains(e.Key)))
            {
                var path = Path.Combine(folder, fileName);
                if (File.Exists(path))
                    File.Delete(path);

                meta.Entries.Remove(fileName);
            }

            RemoveFolderIfEmpty(folder, meta);
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
