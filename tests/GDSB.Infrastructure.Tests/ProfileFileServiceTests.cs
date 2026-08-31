using GDSB.Domain.Entities;
using GDSB.Infrastructure.Encryption.Legacy;
using GDSB.Infrastructure.Encryption.V2;
using GDSB.Infrastructure.Tests.Legacy;
using Xunit;

namespace GDSB.Infrastructure.Tests
{
#pragma warning disable CS0618 // ProfileFileService depende do leitor legado obsoleto de propósito
    public class ProfileFileServiceTests
    {
        private const string Password = "senha-do-cofre-123";

        private readonly ProfileFileService _sut = new(new LegacyV1FileDecryptionService(), new AesGcmFileCryptoService(), new LocalFileSystem());

        // Espelha LocalFileSystem.GetBackupLocation: prefixo "bkp_" no nome do arquivo, não sufixo
        // no fim do path inteiro - ver o porquê no comentário daquele método.
        private static string BackupPathFor(string path, string suffix)
        {
            var directory = Path.GetDirectoryName(path)!;
            return Path.Combine(directory, "bkp_" + Path.GetFileName(path) + suffix);
        }

        private static Profile CreateSampleProfile() => new()
        {
            Nome = "Cofre de teste",
            Boxes = new List<SecretBox>
            {
                new() { Favorito = false, BoxName = "Netflix", Url = "netflix.com", User = "user", Pass = "pass", Obs = "" },
            },
        };

        [Fact]
        public void Open_WithV1File_ReturnsProfileAndFlagsAsLegacy()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);

                var result = _sut.Open(path, Password);

                Assert.True(result.WasLegacyFormat);
                Assert.Equal(profile.Nome, result.Profile.Nome);
                Assert.Equal(profile.Boxes[0].BoxName, result.Profile.Boxes[0].BoxName);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Open_WithV2File_ReturnsProfileAndFlagsAsNotLegacy()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                _sut.Save(path, profile, Password);

                var result = _sut.Open(path, Password);

                Assert.False(result.WasLegacyFormat);
                Assert.Equal(profile.Nome, result.Profile.Nome);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Save_OverwritingV1File_MigratesToV2AndCreatesBackup()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            var backupPath = BackupPathFor(path, ".v1.bak");
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);
                var originalBytes = File.ReadAllBytes(path);

                _sut.Save(path, profile, Password);

                Assert.True(File.Exists(backupPath));
                Assert.Equal(originalBytes, File.ReadAllBytes(backupPath));

                var reopened = _sut.Open(path, Password);
                Assert.False(reopened.WasLegacyFormat);
                Assert.Equal(profile.Nome, reopened.Profile.Nome);
            }
            finally
            {
                File.Delete(path);
                File.Delete(backupPath);
            }
        }

        [Fact]
        public void Save_CalledTwiceAfterMigration_DoesNotOverwriteBackupAgain()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            var backupPath = BackupPathFor(path, ".v1.bak");
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);
                _sut.Save(path, profile, Password);
                var backupAfterFirstSave = File.ReadAllBytes(backupPath);

                profile.Boxes[0].BoxName = "Netflix (editado)";
                _sut.Save(path, profile, Password);

                Assert.Equal(backupAfterFirstSave, File.ReadAllBytes(backupPath));

                var reopened = _sut.Open(path, Password);
                Assert.Equal("Netflix (editado)", reopened.Profile.Boxes[0].BoxName);
            }
            finally
            {
                File.Delete(path);
                File.Delete(backupPath);
            }
        }

        [Fact]
        public void Save_OverwritingV2File_CreatesRollingBakOfPreviousVersion()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            var backupPath = BackupPathFor(path, ".bak");
            try
            {
                var profile = CreateSampleProfile();
                _sut.Save(path, profile, Password);

                profile.Boxes[0].BoxName = "Netflix (editado)";
                _sut.Save(path, profile, Password);

                Assert.True(File.Exists(backupPath));
                var backedUp = _sut.Open(backupPath, Password);
                Assert.Equal("Netflix", backedUp.Profile.Boxes[0].BoxName);

                profile.Boxes[0].BoxName = "Netflix (editado de novo)";
                _sut.Save(path, profile, Password);

                var backedUpAgain = _sut.Open(backupPath, Password);
                Assert.Equal("Netflix (editado)", backedUpAgain.Profile.Boxes[0].BoxName);
            }
            finally
            {
                File.Delete(path);
                File.Delete(backupPath);
            }
        }

        [Fact]
        public void Save_ToFreshlyCreatedEmptyFile_DoesNotCreateSpuriousV1Backup()
        {
            // Reproduz o que o picker de salvar já deixa no disco antes do primeiro Save: no Windows,
            // FileSavePicker.PickSaveFileAsync cria o arquivo de destino vazio (0 bytes); no Android,
            // ActionCreateDocument via SAF faz o mesmo. Um arquivo vazio não é um cofre v1 a migrar.
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            var v1BackupPath = BackupPathFor(path, ".v1.bak");
            var bakPath = BackupPathFor(path, ".bak");
            try
            {
                File.WriteAllBytes(path, Array.Empty<byte>());

                _sut.Save(path, CreateSampleProfile(), Password);

                Assert.False(File.Exists(v1BackupPath));
                Assert.False(File.Exists(bakPath));

                var reopened = _sut.Open(path, Password);
                Assert.False(reopened.WasLegacyFormat);
            }
            finally
            {
                File.Delete(path);
                File.Delete(v1BackupPath);
                File.Delete(bakPath);
            }
        }
    }
#pragma warning restore CS0618
}
