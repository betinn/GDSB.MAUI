using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.Infrastructure.Backup;
using GDSB.Infrastructure.Encryption.Legacy;
using GDSB.Infrastructure.Encryption.V2;
using GDSB.Infrastructure.Tests.Legacy;
using System.Text.Json;
using Xunit;

namespace GDSB.Infrastructure.Tests
{
#pragma warning disable CS0618 // ProfileFileService depende do leitor legado obsoleto de propósito
    public class ProfileFileServiceTests
    {
        private const string Password = "senha-do-cofre-123";

        private readonly string _backupRoot = Path.Combine(Path.GetTempPath(), $"gdsb-backups-{Guid.NewGuid():N}");
        private readonly IVaultBackupStore _backupStore;
        private readonly ProfileFileService _sut;

        public ProfileFileServiceTests()
        {
            _backupStore = new FileSystemVaultBackupStore(_backupRoot);
            _sut = new(new LegacyV1FileDecryptionService(), new AesGcmFileCryptoService(), new LocalFileSystem(), _backupStore);
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
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);
                var originalBytes = File.ReadAllBytes(path);

                _sut.Save(path, profile, Password);

                var backup = Assert.Single(_backupStore.List(), b => b.OriginLocation == path);
                Assert.Equal(VaultBackupKind.LegacyV1, backup.Kind);
                Assert.Equal(originalBytes, _backupStore.Read(backup));

                var reopened = _sut.Open(path, Password);
                Assert.False(reopened.WasLegacyFormat);
                Assert.Equal(profile.Nome, reopened.Profile.Nome);
            }
            finally
            {
                File.Delete(path);
                _backupStore.DeleteAllFor(path);
            }
        }

        [Fact]
        public void Save_CalledTwiceAfterMigration_DoesNotOverwriteBackupAgain()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);
                _sut.Save(path, profile, Password);
                var legacyBackup = Assert.Single(_backupStore.List(), b => b.OriginLocation == path);
                var backupAfterFirstSave = _backupStore.Read(legacyBackup);

                profile.Boxes[0].BoxName = "Netflix (editado)";
                _sut.Save(path, profile, Password);

                // O segundo Save já parte de um arquivo v2 (migrado no primeiro), então também
                // cria um backup Rolling - o que importa aqui é que o LegacyV1 continua intacto.
                var legacyBackupAfterSecondSave = Assert.Single(
                    _backupStore.List(),
                    b => b.OriginLocation == path && b.Kind == VaultBackupKind.LegacyV1);
                Assert.Equal(backupAfterFirstSave, _backupStore.Read(legacyBackupAfterSecondSave));

                var reopened = _sut.Open(path, Password);
                Assert.Equal("Netflix (editado)", reopened.Profile.Boxes[0].BoxName);
            }
            finally
            {
                File.Delete(path);
                _backupStore.DeleteAllFor(path);
            }
        }

        [Fact]
        public void Save_OverwritingV2File_CreatesRollingBakOfPreviousVersion()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                _sut.Save(path, profile, Password);

                profile.Boxes[0].BoxName = "Netflix (editado)";
                _sut.Save(path, profile, Password);

                var backup = Assert.Single(_backupStore.List(), b => b.OriginLocation == path);
                Assert.Equal(VaultBackupKind.Rolling, backup.Kind);
                var backedUp = _sut.Open(backup.Id, Password);
                Assert.Equal("Netflix", backedUp.Profile.Boxes[0].BoxName);

                profile.Boxes[0].BoxName = "Netflix (editado de novo)";
                _sut.Save(path, profile, Password);

                var backupAgain = Assert.Single(_backupStore.List(), b => b.OriginLocation == path);
                var backedUpAgain = _sut.Open(backupAgain.Id, Password);
                Assert.Equal("Netflix (editado)", backedUpAgain.Profile.Boxes[0].BoxName);
            }
            finally
            {
                File.Delete(path);
                _backupStore.DeleteAllFor(path);
            }
        }

        [Fact]
        public void Save_RoundTripsVaultSettings()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                profile.Settings = new VaultSettings
                {
                    ClipboardClearEnabled = false,
                    ClipboardClearSeconds = 90,
                    AutoLockEnabled = false,
                    AutoLockMinutes = 15,
                };

                _sut.Save(path, profile, Password);
                var reopened = _sut.Open(path, Password);

                Assert.False(reopened.Profile.Settings.ClipboardClearEnabled);
                Assert.Equal(90, reopened.Profile.Settings.ClipboardClearSeconds);
                Assert.False(reopened.Profile.Settings.AutoLockEnabled);
                Assert.Equal(15, reopened.Profile.Settings.AutoLockMinutes);
            }
            finally
            {
                File.Delete(path);
                _backupStore.DeleteAllFor(path);
            }
        }

        [Fact]
        public void Open_V2FileWithoutSettingsKey_UsesDefaultVaultSettings()
        {
            // Reproduz um arquivo v2 gravado antes da chave "Settings" existir - o inicializador
            // de propriedade de VaultSettings deve valer quando a chave não está no JSON.
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var legacyJson = JsonSerializer.Serialize(new { Nome = "Cofre de teste", Boxes = Array.Empty<object>() });
                var fileBytes = new AesGcmFileCryptoService().Encrypt(legacyJson, Password);
                File.WriteAllBytes(path, fileBytes);

                var result = _sut.Open(path, Password);

                Assert.True(result.Profile.Settings.ClipboardClearEnabled);
                Assert.Equal(20, result.Profile.Settings.ClipboardClearSeconds);
                Assert.True(result.Profile.Settings.AutoLockEnabled);
                Assert.Equal(2, result.Profile.Settings.AutoLockMinutes);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Save_ToFreshlyCreatedEmptyFile_DoesNotCreateSpuriousV1Backup()
        {
            // Reproduz o que o picker de salvar já deixa no disco antes do primeiro Save: no Windows,
            // FileSavePicker.PickSaveFileAsync cria o arquivo de destino vazio (0 bytes); no Android,
            // ActionCreateDocument via SAF faz o mesmo. Um arquivo vazio não é um cofre v1 a migrar.
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                File.WriteAllBytes(path, Array.Empty<byte>());

                _sut.Save(path, CreateSampleProfile(), Password);

                Assert.DoesNotContain(_backupStore.List(), b => b.OriginLocation == path);

                var reopened = _sut.Open(path, Password);
                Assert.False(reopened.WasLegacyFormat);
            }
            finally
            {
                File.Delete(path);
                _backupStore.DeleteAllFor(path);
            }
        }
    }
#pragma warning restore CS0618
}
