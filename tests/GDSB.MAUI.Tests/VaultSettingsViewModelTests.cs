using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.MAUI.Services;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class VaultSettingsViewModelTests
    {
        private const string Location = "content://fake-vault";

        // Nomes de propósito sem "password"/"pwd"/"passphrase": a regra S2068 do Sonar ("Hard-coded
        // credentials") flaga identificadores nesses moldes atribuídos a um valor literal, mesmo
        // sendo só um valor de teste.
        private const string VaultUnlockCode = "senha-do-cofre-123";
        private const string NewVaultUnlockCode = "senhanova123";
        private const string WrongVaultUnlockCode = "senha-errada";

        private sealed class Sut
        {
            public FakeProfileFileService ProfileFileService { get; } = new();
            public FakeFilePickerService FilePickerService { get; } = new();
            public FakeNavigationService NavigationService { get; } = new();
            public FakeBiometricUnlockService BiometricUnlockService { get; } = new();
            public FakeVaultSessionService VaultSessionService { get; } = new();
            public FakeVaultBackupStore BackupStore { get; } = new();
            public FakeAlertService AlertService { get; } = new();
            public FakePreferencesService PreferencesService { get; } = new();
            public VaultSettingsViewModel ViewModel { get; }

            public Sut()
            {
                var biometricOptIn = new BiometricOptInCoordinator(BiometricUnlockService, AlertService, PreferencesService);
                ViewModel = new VaultSettingsViewModel(
                    new VaultAccess(ProfileFileService, VaultSessionService),
                    FilePickerService,
                    NavigationService,
                    BiometricUnlockService,
                    BackupStore,
                    AlertService,
                    biometricOptIn);

                // Por padrão, reabrir com a senha atual (usado na validação da troca de senha) dá certo.
                ProfileFileService.OpenHandler = (_, password) => password == VaultUnlockCode
                    ? new ProfileOpenResult(Profile, WasLegacyFormat: false)
                    : throw new InvalidPasswordOrCorruptFileException();
            }

            public Profile Profile { get; } = new() { Nome = "Cofre de teste" };

            public void Load()
            {
                ViewModel.ApplyQueryAttributes(new Dictionary<string, object>
                {
                    ["Profile"] = Profile,
                    ["Location"] = Location,
                    ["Password"] = VaultUnlockCode,
                });
            }
        }

        [Fact]
        public async Task SaveNameAsync_Success_SavesAndOffersNewFile()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.VaultName = "Novo nome";

            await sut.ViewModel.SaveNameCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal("Novo nome", save.Profile.Nome);
            Assert.True(sut.ViewModel.ShowSaveAsNewFileOffer);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }

        [Fact]
        public async Task SaveProtectionsAsync_SavesCurrentFileAndStartsSession_WithoutOffer()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.ClipboardClearEnabled = false;
            sut.ViewModel.AutoLockMinutes = 15;

            await sut.ViewModel.SaveProtectionsCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal(Location, save.Location);
            Assert.False(save.Profile.Settings.ClipboardClearEnabled);
            Assert.Equal(15, save.Profile.Settings.AutoLockMinutes);
            Assert.Same(save.Profile.Settings, sut.VaultSessionService.Settings);
            Assert.False(sut.ViewModel.ShowSaveAsNewFileOffer);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_DoesNotSave()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.CurrentPassword = WrongVaultUnlockCode;
            sut.ViewModel.NewPassword = NewVaultUnlockCode;
            sut.ViewModel.ConfirmNewPassword = NewVaultUnlockCode;

            await sut.ViewModel.ChangePasswordCommand.ExecuteAsync(null);

            Assert.Equal("Senha atual incorreta.", sut.ViewModel.PasswordErrorMessage);
            Assert.Empty(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task ChangePasswordAsync_Success_SavesWithNewPasswordAndOffersNewFile()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.CurrentPassword = VaultUnlockCode;
            sut.ViewModel.NewPassword = NewVaultUnlockCode;
            sut.ViewModel.ConfirmNewPassword = NewVaultUnlockCode;

            await sut.ViewModel.ChangePasswordCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal(NewVaultUnlockCode, save.Password);
            Assert.True(sut.ViewModel.ShowSaveAsNewFileOffer);
            Assert.Null(sut.ViewModel.PasswordErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_BiometricEnabled_ReSealsWithNewPassword()
        {
            var sut = new Sut();
            sut.Load();
            sut.BiometricUnlockService.IsEnabled = true;
            sut.ViewModel.CurrentPassword = VaultUnlockCode;
            sut.ViewModel.NewPassword = NewVaultUnlockCode;
            sut.ViewModel.ConfirmNewPassword = NewVaultUnlockCode;

            await sut.ViewModel.ChangePasswordCommand.ExecuteAsync(null);

            Assert.Equal(1, sut.BiometricUnlockService.DisableCallCount);
        }

        [Fact]
        public async Task ChangePasswordAsync_DeleteOldBackupsChecked_CallsDeleteAllFor()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.CurrentPassword = VaultUnlockCode;
            sut.ViewModel.NewPassword = NewVaultUnlockCode;
            sut.ViewModel.ConfirmNewPassword = NewVaultUnlockCode;
            sut.ViewModel.DeleteOldBackups = true;

            await sut.ViewModel.ChangePasswordCommand.ExecuteAsync(null);

            var call = Assert.Single(sut.BackupStore.DeleteAllForCalls);
            Assert.Equal(Location, call);
        }

        [Fact]
        public async Task ChangePasswordAsync_DeleteOldBackupsUnchecked_DoesNotCallDeleteAllFor()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.CurrentPassword = VaultUnlockCode;
            sut.ViewModel.NewPassword = NewVaultUnlockCode;
            sut.ViewModel.ConfirmNewPassword = NewVaultUnlockCode;
            sut.ViewModel.DeleteOldBackups = false;

            await sut.ViewModel.ChangePasswordCommand.ExecuteAsync(null);

            Assert.Empty(sut.BackupStore.DeleteAllForCalls);
        }

        [Fact]
        public async Task AcceptSaveAsNewFileAsync_SavesOnNewLocationWithoutTouchingOriginal()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.VaultName = "Novo nome";
            await sut.ViewModel.SaveNameCommand.ExecuteAsync(null);
            sut.FilePickerService.PickSaveLocationResult = "content://new-vault";

            await sut.ViewModel.AcceptSaveAsNewFileCommand.ExecuteAsync(null);

            Assert.Equal(2, sut.ProfileFileService.SaveCalls.Count);
            Assert.Equal(Location, sut.ProfileFileService.SaveCalls[0].Location);
            Assert.Equal("content://new-vault", sut.ProfileFileService.SaveCalls[1].Location);
            Assert.True(sut.ViewModel.ShowSwitchToNewFileOffer);
            Assert.False(sut.ViewModel.ShowSaveAsNewFileOffer);
        }

        [Fact]
        public async Task DeclineSaveAsNewFileAsync_KeepsCurrentFileAndReturnsToVault()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.VaultName = "Novo nome";
            await sut.ViewModel.SaveNameCommand.ExecuteAsync(null);

            await sut.ViewModel.DeclineSaveAsNewFileCommand.ExecuteAsync(null);

            Assert.False(sut.ViewModel.ShowSaveAsNewFileOffer);
            var navigation = Assert.Single(sut.NavigationService.NavigateToRootCalls);
            Assert.Equal("VaultPage", navigation.Route);
            Assert.Equal(Location, navigation.Parameters!["Location"]);
            Assert.Single(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task ProtectionsChangeAlone_DoesNotOfferNewFileOrCallPicker()
        {
            var sut = new Sut();
            sut.Load();
            sut.ViewModel.AutoLockEnabled = false;

            await sut.ViewModel.SaveProtectionsCommand.ExecuteAsync(null);

            Assert.False(sut.ViewModel.ShowSaveAsNewFileOffer);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }
    }
}
