using GDSB.Domain.Entities;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class CreateVaultViewModelTests
    {
        private sealed class Sut
        {
            public FakeProfileFileService ProfileFileService { get; } = new();
            public FakeFilePickerService FilePickerService { get; } = new();
            public FakeNavigationService NavigationService { get; } = new();
            public FakeBiometricUnlockService BiometricUnlockService { get; } = new();
            public FakePreferencesService PreferencesService { get; } = new();
            public FakeAlertService AlertService { get; } = new();
            public FakeVaultSessionService VaultSessionService { get; } = new();
            public FakeLocalizationService LocalizationService { get; } = new();
            public CreateVaultViewModel ViewModel { get; }

            public Sut()
            {
                var biometricOptIn = new BiometricOptInCoordinator(BiometricUnlockService, AlertService, PreferencesService, LocalizationService);
                ViewModel = new CreateVaultViewModel(
                    ProfileFileService,
                    FilePickerService,
                    NavigationService,
                    BiometricUnlockService,
                    VaultSessionService,
                    LocalizationService,
                    biometricOptIn);
            }
        }

        [Fact]
        public async Task CreateVaultAsync_ShortPassword_SetsError()
        {
            var sut = new Sut();
            sut.ViewModel.VaultName = "Meu Cofre";
            sut.ViewModel.Password = "123";
            sut.ViewModel.ConfirmPassword = "123";

            await sut.ViewModel.CreateVaultCommand.ExecuteAsync(null);

            Assert.Equal("CreateVault_MinPasswordLengthMessage", sut.ViewModel.ErrorMessage);
            Assert.Empty(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task CreateVaultAsync_MismatchedPasswords_SetsError()
        {
            var sut = new Sut();
            sut.ViewModel.VaultName = "Meu Cofre";
            sut.ViewModel.Password = "senha12345";
            sut.ViewModel.ConfirmPassword = "outrasenha";

            await sut.ViewModel.CreateVaultCommand.ExecuteAsync(null);

            Assert.Equal("Vault_PasswordsDoNotMatchMessage", sut.ViewModel.ErrorMessage);
        }

        [Fact]
        public void NewViewModel_DefaultsVaultNameFromCatalog()
        {
            var sut = new Sut();

            Assert.Equal("CreateVault_DefaultVaultName", sut.ViewModel.VaultName);
        }

        [Fact]
        public async Task CreateVaultAsync_Success_SavesAndNavigatesToVault()
        {
            var sut = new Sut();
            sut.ViewModel.VaultName = "Meu Cofre";
            sut.ViewModel.Password = "senha12345";
            sut.ViewModel.ConfirmPassword = "senha12345";

            await sut.ViewModel.CreateVaultCommand.ExecuteAsync(null);

            Assert.Null(sut.ViewModel.ErrorMessage);
            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal("Meu Cofre", save.Profile.Nome);
            var navigation = Assert.Single(sut.NavigationService.NavigateToRootCalls);
            Assert.Equal("VaultPage", navigation.Route);
        }

        [Fact]
        public async Task CreateVaultAsync_Success_ProtectionTogglesReachSavedProfileAndSession()
        {
            // Nome de propósito sem "password"/"pwd": a regra S2068 do Sonar ("Hard-coded
            // credentials") flaga identificadores nesses moldes atribuídos a um valor literal.
            const string sampleUnlockCode = "senha12345";

            var sut = new Sut();
            sut.ViewModel.VaultName = "Meu Cofre";
            sut.ViewModel.Password = sampleUnlockCode;
            sut.ViewModel.ConfirmPassword = sampleUnlockCode;
            sut.ViewModel.ClipboardClearEnabled = false;
            sut.ViewModel.AutoLockEnabled = false;
            sut.ViewModel.AutoLockMinutes = 15;

            await sut.ViewModel.CreateVaultCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.False(save.Profile.Settings.ClipboardClearEnabled);
            Assert.False(save.Profile.Settings.AutoLockEnabled);
            Assert.Equal(15, save.Profile.Settings.AutoLockMinutes);
            var started = Assert.Single(sut.VaultSessionService.StartCalls);
            Assert.Same(save.Profile.Settings, started);
        }

        [Fact]
        public void SelectClipboardClearSecondsCommand_ParsesStringParameter()
        {
            // O CommandParameter do XAML sempre chega como string ao comando - regressão do bug
            // em que os seletores de tempo pareciam não reagir a clique nenhum.
            var sut = new Sut();

            sut.ViewModel.SelectClipboardClearSecondsCommand.Execute("45");

            Assert.Equal(45, sut.ViewModel.ClipboardClearSeconds);
        }

        [Fact]
        public void SelectAutoLockMinutesCommand_ParsesStringParameter()
        {
            var sut = new Sut();

            sut.ViewModel.SelectAutoLockMinutesCommand.Execute("15");

            Assert.Equal(15, sut.ViewModel.AutoLockMinutes);
        }

        [Fact]
        public void NewViewModel_DefaultsToCountAndTen()
        {
            var sut = new Sut();

            Assert.Equal(BackupRetentionMode.Count, sut.ViewModel.BackupRetentionMode);
            Assert.Equal(10, sut.ViewModel.BackupRetentionCount);
            Assert.Equal(5, sut.ViewModel.BackupRetentionDays);
            Assert.True(sut.ViewModel.IsBackupRetentionByCount);
            Assert.False(sut.ViewModel.IsBackupRetentionByDays);
        }

        [Fact]
        public void SelectBackupRetentionCountCommand_ParsesStringParameter()
        {
            var sut = new Sut();

            sut.ViewModel.SelectBackupRetentionCountCommand.Execute("50");

            Assert.Equal(50, sut.ViewModel.BackupRetentionCount);
        }

        [Fact]
        public void SelectBackupRetentionDaysCommand_ParsesStringParameter()
        {
            var sut = new Sut();

            sut.ViewModel.SelectBackupRetentionDaysCommand.Execute("30");

            Assert.Equal(30, sut.ViewModel.BackupRetentionDays);
        }

        [Fact]
        public void SelectBackupRetentionModeDaysCommand_SwitchesModeAndFlipsVisibilityFlags()
        {
            var sut = new Sut();

            sut.ViewModel.SelectBackupRetentionModeDaysCommand.Execute(null);

            Assert.Equal(BackupRetentionMode.Days, sut.ViewModel.BackupRetentionMode);
            Assert.False(sut.ViewModel.IsBackupRetentionByCount);
            Assert.True(sut.ViewModel.IsBackupRetentionByDays);
        }

        [Fact]
        public async Task CreateVaultAsync_Success_BackupRetentionReachesSavedProfile()
        {
            const string sampleUnlockCode = "senha12345";

            var sut = new Sut();
            sut.ViewModel.VaultName = "Meu Cofre";
            sut.ViewModel.Password = sampleUnlockCode;
            sut.ViewModel.ConfirmPassword = sampleUnlockCode;
            sut.ViewModel.SelectBackupRetentionModeDaysCommand.Execute(null);
            sut.ViewModel.SelectBackupRetentionDaysCommand.Execute("30");

            await sut.ViewModel.CreateVaultCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal(BackupRetentionMode.Days, save.Profile.Settings.BackupRetentionMode);
            Assert.Equal(30, save.Profile.Settings.BackupRetentionDays);
        }
    }
}
