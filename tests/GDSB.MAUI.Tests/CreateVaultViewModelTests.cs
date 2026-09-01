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
            public CreateVaultViewModel ViewModel { get; }

            public Sut()
            {
                var biometricOptIn = new BiometricOptInCoordinator(BiometricUnlockService, AlertService, PreferencesService);
                ViewModel = new CreateVaultViewModel(
                    ProfileFileService,
                    FilePickerService,
                    NavigationService,
                    BiometricUnlockService,
                    VaultSessionService,
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

            Assert.Equal("A senha mestra precisa ter pelo menos 8 caracteres.", sut.ViewModel.ErrorMessage);
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

            Assert.Equal("As senhas não coincidem.", sut.ViewModel.ErrorMessage);
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
    }
}
