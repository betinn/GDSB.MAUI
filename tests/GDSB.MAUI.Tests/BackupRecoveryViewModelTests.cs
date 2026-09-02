using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class BackupRecoveryViewModelTests
    {
        private const string Location = "content://fake-vault";

        // Nomes de propósito sem "password"/"pwd"/"passphrase": a regra S2068 do Sonar ("Hard-coded
        // credentials") flaga identificadores nesses moldes atribuídos a um valor literal, mesmo
        // sendo só um valor de teste.
        private const string VaultUnlockCode = "senha-do-backup-123";
        private const string WrongVaultUnlockCode = "senha-errada";

        private sealed class Sut
        {
            public FakeVaultBackupStore BackupStore { get; } = new();
            public FakeProfileFileService ProfileFileService { get; } = new();
            public FakeFilePickerService FilePickerService { get; } = new();
            public FakeNavigationService NavigationService { get; } = new();
            public FakeVaultSessionService VaultSessionService { get; } = new();
            public BackupRecoveryViewModel ViewModel { get; }

            public Profile Profile { get; } = new() { Nome = "Cofre de teste" };

            public Sut()
            {
                ViewModel = new BackupRecoveryViewModel(
                    BackupStore,
                    ProfileFileService,
                    FilePickerService,
                    NavigationService,
                    VaultSessionService);

                ProfileFileService.OpenHandler = (_, password) => password == VaultUnlockCode
                    ? new ProfileOpenResult(Profile, WasLegacyFormat: false)
                    : throw new InvalidPasswordOrCorruptFileException();
            }

            public VaultBackupInfo AddBackup() => BackupStore.Store(Location, Profile.Nome, new byte[] { 1, 2, 3 }, VaultBackupKind.Rolling);
        }

        [Fact]
        public void Initialize_ListsBackupsFromStore()
        {
            var sut = new Sut();
            var info = sut.AddBackup();

            sut.ViewModel.Initialize();

            var item = Assert.Single(sut.ViewModel.Backups);
            Assert.Equal(info, item.Info);
            Assert.True(sut.ViewModel.HasBackups);
        }

        [Fact]
        public async Task ConfirmRestoreAsync_WrongPassword_DoesNotRestore_ShowsGenericMessage()
        {
            var sut = new Sut();
            sut.AddBackup();
            sut.ViewModel.Initialize();
            var item = sut.ViewModel.Backups[0];

            sut.ViewModel.BeginRestoreCommand.Execute(item);
            sut.ViewModel.RestorePassword = WrongVaultUnlockCode;
            await sut.ViewModel.ConfirmRestoreCommand.ExecuteAsync(null);

            Assert.Equal("Senha incorreta ou arquivo corrompido.", sut.ViewModel.RestoreErrorMessage);
            Assert.Empty(sut.ProfileFileService.SaveCalls);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
            Assert.True(sut.ViewModel.IsRestoring);
        }

        [Fact]
        public async Task ConfirmRestoreAsync_Success_SavesAtPickedLocationAndNavigates()
        {
            var sut = new Sut();
            sut.AddBackup();
            sut.ViewModel.Initialize();
            var item = sut.ViewModel.Backups[0];
            sut.FilePickerService.PickSaveLocationResult = "content://restored-vault";

            sut.ViewModel.BeginRestoreCommand.Execute(item);
            sut.ViewModel.RestorePassword = VaultUnlockCode;
            await sut.ViewModel.ConfirmRestoreCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal("content://restored-vault", save.Location);
            Assert.Equal(VaultUnlockCode, save.Password);
            Assert.Same(sut.Profile, save.Profile);

            var navigate = Assert.Single(sut.NavigationService.NavigateToRootCalls);
            Assert.Equal("VaultPage", navigate.Route);
            Assert.Equal("content://restored-vault", navigate.Parameters!["Location"]);

            Assert.False(sut.ViewModel.IsRestoring);
        }

        [Fact]
        public async Task ConfirmRestoreAsync_UserCancelsFilePicker_DoesNotSaveOrNavigate()
        {
            var sut = new Sut();
            sut.AddBackup();
            sut.ViewModel.Initialize();
            var item = sut.ViewModel.Backups[0];
            sut.FilePickerService.PickSaveLocationResult = string.Empty;

            sut.ViewModel.BeginRestoreCommand.Execute(item);
            sut.ViewModel.RestorePassword = VaultUnlockCode;
            await sut.ViewModel.ConfirmRestoreCommand.ExecuteAsync(null);

            Assert.Empty(sut.ProfileFileService.SaveCalls);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }

        [Fact]
        public void ConfirmDelete_CallsStoreDelete()
        {
            var sut = new Sut();
            var info = sut.AddBackup();
            sut.ViewModel.Initialize();
            var item = sut.ViewModel.Backups[0];

            sut.ViewModel.PromptDeleteCommand.Execute(item);
            sut.ViewModel.ConfirmDeleteCommand.Execute(null);

            Assert.DoesNotContain(info, sut.BackupStore.Items);
            Assert.Empty(sut.ViewModel.Backups);
            Assert.False(sut.ViewModel.IsConfirmingDelete);
        }

        [Fact]
        public void ConfirmDeleteAll_DeletesEveryListedBackup()
        {
            var sut = new Sut();
            sut.AddBackup();
            sut.BackupStore.Store("content://other-vault", "Outro cofre", new byte[] { 4, 5 }, VaultBackupKind.Rolling);
            sut.ViewModel.Initialize();

            sut.ViewModel.PromptDeleteAllCommand.Execute(null);
            sut.ViewModel.ConfirmDeleteAllCommand.Execute(null);

            Assert.Empty(sut.BackupStore.Items);
            Assert.Empty(sut.ViewModel.Backups);
            Assert.False(sut.ViewModel.IsConfirmingDeleteAll);
        }
    }
}
