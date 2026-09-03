using System.Text;
using GDSB.Domain.Entities;
using GDSB.Infrastructure.Backup;
using Xunit;

namespace GDSB.Infrastructure.Tests.Backup
{
    public class FileSystemVaultBackupStoreTests : IDisposable
    {
        private const string Origin = "content://fake-vault";
        private const string VaultName = "Cofre de teste";

        private static readonly BackupRetentionPolicy DefaultRetention = new(BackupRetentionMode.Count, 10, 5);

        private readonly string _root = Path.Combine(Path.GetTempPath(), $"gdsb-backups-{Guid.NewGuid():N}");
        private readonly FileSystemVaultBackupStore _sut;

        // Relógio falso, avançado manualmente entre saves - é o que torna determinístico tanto o
        // "timestamps distintos" (poda por quantidade) quanto a poda por dias.
        private DateTime _now = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);

        public FileSystemVaultBackupStoreTests()
        {
            _sut = new FileSystemVaultBackupStore(_root, () => _now);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

        private void Tick(int seconds = 1) => _now = _now.AddSeconds(seconds);

        [Fact]
        public void Store_Rolling_AccumulatesVersionsInsteadOfOverwriting()
        {
            var first = _sut.Store(Origin, VaultName, Bytes("versão 1"), VaultBackupKind.Rolling, DefaultRetention);
            Tick();
            var second = _sut.Store(Origin, VaultName, Bytes("versão 2"), VaultBackupKind.Rolling, DefaultRetention);

            var backups = _sut.List();

            Assert.Equal(2, backups.Count);
            Assert.NotEqual(first.Id, second.Id);
            Assert.Equal("versão 1", Encoding.UTF8.GetString(_sut.Read(first)));
            Assert.Equal("versão 2", Encoding.UTF8.GetString(_sut.Read(second)));
        }

        [Fact]
        public void Store_Rolling_SameSecondCollision_SuffixesTheFileName()
        {
            var first = _sut.Store(Origin, VaultName, Bytes("versão 1"), VaultBackupKind.Rolling, DefaultRetention);
            var second = _sut.Store(Origin, VaultName, Bytes("versão 2"), VaultBackupKind.Rolling, DefaultRetention);

            Assert.NotEqual(first.Id, second.Id);
            Assert.Equal(2, _sut.List().Count);
        }

        [Fact]
        public void Store_Rolling_CountMode_KeepsExactlyTheConfiguredNumberOfVersions()
        {
            var retention = new BackupRetentionPolicy(BackupRetentionMode.Count, 3, 5);

            for (var i = 1; i <= 5; i++)
            {
                _sut.Store(Origin, VaultName, Bytes($"versão {i}"), VaultBackupKind.Rolling, retention);
                Tick();
            }

            var backups = _sut.List();

            Assert.Equal(3, backups.Count);
            Assert.Equal(3, backups.Select(b => b.CreatedAtUtc).Distinct().Count());
            Assert.DoesNotContain(backups, b => Encoding.UTF8.GetString(_sut.Read(b)) == "versão 1");
            Assert.DoesNotContain(backups, b => Encoding.UTF8.GetString(_sut.Read(b)) == "versão 2");
            Assert.Contains(backups, b => Encoding.UTF8.GetString(_sut.Read(b)) == "versão 5");
        }

        [Fact]
        public void Store_Rolling_DaysMode_PrunesVersionsOlderThanTheLimit()
        {
            var retention = new BackupRetentionPolicy(BackupRetentionMode.Days, 10, 5);

            _sut.Store(Origin, VaultName, Bytes("antigo"), VaultBackupKind.Rolling, retention);
            _now = _now.AddDays(6);
            var recent = _sut.Store(Origin, VaultName, Bytes("recente"), VaultBackupKind.Rolling, retention);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(recent.Id, backup.Id);
        }

        [Fact]
        public void Store_Rolling_DaysMode_StillRespectsHardCeiling()
        {
            var retention = new BackupRetentionPolicy(BackupRetentionMode.Days, 10, 36500);

            for (var i = 0; i < BackupRetentionPolicy.HardCeiling + 5; i++)
            {
                _sut.Store(Origin, VaultName, Bytes($"versão {i}"), VaultBackupKind.Rolling, retention);
                Tick();
            }

            Assert.Equal(BackupRetentionPolicy.HardCeiling, _sut.List().Count);
        }

        [Fact]
        public void Store_Rolling_NeverPrunesTheMostRecentVersion_EvenWhenPolicyWouldDeleteEverything()
        {
            var retention = new BackupRetentionPolicy(BackupRetentionMode.Count, 0, 5);

            _sut.Store(Origin, VaultName, Bytes("v1"), VaultBackupKind.Rolling, retention);
            Tick();
            var latest = _sut.Store(Origin, VaultName, Bytes("v2"), VaultBackupKind.Rolling, retention);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(latest.Id, backup.Id);
        }

        [Fact]
        public void Store_LegacyV1_NeverOverwritesExistingBackup()
        {
            var first = _sut.Store(Origin, VaultName, Bytes("original v1"), VaultBackupKind.LegacyV1, DefaultRetention);
            _sut.Store(Origin, VaultName, Bytes("tentativa de sobrescrever"), VaultBackupKind.LegacyV1, DefaultRetention);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(first.Id, backup.Id);
            Assert.Equal("original v1", Encoding.UTF8.GetString(_sut.Read(backup)));
        }

        [Fact]
        public void Store_LegacyV1_IsNeverPrunedAndDoesNotCountTowardRollingRetention()
        {
            var retention = new BackupRetentionPolicy(BackupRetentionMode.Count, 1, 5);

            var legacy = _sut.Store(Origin, VaultName, Bytes("original v1"), VaultBackupKind.LegacyV1, retention);
            _sut.Store(Origin, VaultName, Bytes("rolling 1"), VaultBackupKind.Rolling, retention);
            Tick();
            _sut.Store(Origin, VaultName, Bytes("rolling 2"), VaultBackupKind.Rolling, retention);

            var backups = _sut.List();

            Assert.Equal(2, backups.Count);
            Assert.Contains(backups, b => b.Id == legacy.Id);
            Assert.Contains(backups, b => b.Kind == VaultBackupKind.Rolling
                && Encoding.UTF8.GetString(_sut.Read(b)) == "rolling 2");
        }

        [Fact]
        public void List_ReturnsReadableNameAndOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("conteúdo"), VaultBackupKind.Rolling, DefaultRetention);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(Origin, backup.OriginLocation);
            Assert.Equal(VaultName, backup.VaultName);
            Assert.StartsWith(VaultBackupNaming.Prefix, backup.DisplayName);
            Assert.EndsWith(VaultBackupNaming.RollingSuffix, backup.DisplayName);
        }

        [Fact]
        public void List_ReturnsBothKindsForSameOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling, DefaultRetention);
            _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1, DefaultRetention);

            var backups = _sut.List();

            Assert.Equal(2, backups.Count);
            Assert.Contains(backups, b => b.Kind == VaultBackupKind.Rolling);
            Assert.Contains(backups, b => b.Kind == VaultBackupKind.LegacyV1);
        }

        [Fact]
        public void Delete_RemovesOnlyThatBackup()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling, DefaultRetention);
            var legacy = _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1, DefaultRetention);

            _sut.Delete(legacy);

            var remaining = Assert.Single(_sut.List());
            Assert.Equal(VaultBackupKind.Rolling, remaining.Kind);
            Assert.False(File.Exists(legacy.Id));
        }

        [Fact]
        public void DeleteAllFor_RemovesEveryBackupOfThatOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling, DefaultRetention);
            _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1, DefaultRetention);
            _sut.Store("content://other-vault", "Outro cofre", Bytes("outro"), VaultBackupKind.Rolling, DefaultRetention);

            _sut.DeleteAllFor(Origin);

            var remaining = Assert.Single(_sut.List());
            Assert.Equal("content://other-vault", remaining.OriginLocation);
        }
    }
}
