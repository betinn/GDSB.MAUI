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

        private readonly string _root = Path.Combine(Path.GetTempPath(), $"gdsb-backups-{Guid.NewGuid():N}");
        private readonly FileSystemVaultBackupStore _sut;

        public FileSystemVaultBackupStoreTests()
        {
            _sut = new FileSystemVaultBackupStore(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

        [Fact]
        public void Store_Rolling_OverwritesPreviousBackup()
        {
            _sut.Store(Origin, VaultName, Bytes("versão 1"), VaultBackupKind.Rolling);
            var second = _sut.Store(Origin, VaultName, Bytes("versão 2"), VaultBackupKind.Rolling);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(second.Id, backup.Id);
            Assert.Equal("versão 2", Encoding.UTF8.GetString(_sut.Read(backup)));
        }

        [Fact]
        public void Store_LegacyV1_NeverOverwritesExistingBackup()
        {
            var first = _sut.Store(Origin, VaultName, Bytes("original v1"), VaultBackupKind.LegacyV1);
            _sut.Store(Origin, VaultName, Bytes("tentativa de sobrescrever"), VaultBackupKind.LegacyV1);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(first.Id, backup.Id);
            Assert.Equal("original v1", Encoding.UTF8.GetString(_sut.Read(backup)));
        }

        [Fact]
        public void List_ReturnsReadableNameAndOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("conteúdo"), VaultBackupKind.Rolling);

            var backup = Assert.Single(_sut.List());
            Assert.Equal(Origin, backup.OriginLocation);
            Assert.Equal(VaultName, backup.VaultName);
            Assert.StartsWith(VaultBackupNaming.Prefix, backup.DisplayName);
            Assert.EndsWith(VaultBackupNaming.RollingSuffix, backup.DisplayName);
        }

        [Fact]
        public void List_ReturnsBothKindsForSameOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling);
            _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1);

            var backups = _sut.List();

            Assert.Equal(2, backups.Count);
            Assert.Contains(backups, b => b.Kind == VaultBackupKind.Rolling);
            Assert.Contains(backups, b => b.Kind == VaultBackupKind.LegacyV1);
        }

        [Fact]
        public void Delete_RemovesOnlyThatBackup()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling);
            var legacy = _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1);

            _sut.Delete(legacy);

            var remaining = Assert.Single(_sut.List());
            Assert.Equal(VaultBackupKind.Rolling, remaining.Kind);
            Assert.False(File.Exists(legacy.Id));
        }

        [Fact]
        public void DeleteAllFor_RemovesEveryBackupOfThatOrigin()
        {
            _sut.Store(Origin, VaultName, Bytes("rolling"), VaultBackupKind.Rolling);
            _sut.Store(Origin, VaultName, Bytes("legacy"), VaultBackupKind.LegacyV1);
            _sut.Store("content://other-vault", "Outro cofre", Bytes("outro"), VaultBackupKind.Rolling);

            _sut.DeleteAllFor(Origin);

            var remaining = Assert.Single(_sut.List());
            Assert.Equal("content://other-vault", remaining.OriginLocation);
        }
    }
}
