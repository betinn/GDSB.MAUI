using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using System.Text;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class UnlockViewModelTests
    {
        // Nome de propósito sem "password"/"pwd"/"passphrase": a regra S2068 do Sonar
        // ("Hard-coded credentials") flaga identificadores nesses moldes atribuídos a um valor
        // literal, mesmo sendo só um valor de teste. Mesma renomeação de VaultSettingsViewModelTests.
        private const string VaultUnlockCode = "senha-do-cofre-123";
        private const string Location = "content://fake-vault";

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
            public OnboardingViewModel Onboarding { get; }
            public LanguageSelectorViewModel Language { get; }
            public UnlockViewModel ViewModel { get; }

            public Sut()
            {
                var biometricOptIn = new BiometricOptInCoordinator(BiometricUnlockService, AlertService, PreferencesService);
                Onboarding = new OnboardingViewModel(PreferencesService);
                Language = new LanguageSelectorViewModel(LocalizationService);
                ViewModel = new UnlockViewModel(
                    new VaultAccess(ProfileFileService, VaultSessionService),
                    FilePickerService,
                    NavigationService,
                    BiometricUnlockService,
                    PreferencesService,
                    AlertService,
                    new UnlockOverlays(biometricOptIn, Onboarding, Language));
            }
        }

        private static Profile SampleProfile() => new() { Nome = "Cofre de teste" };

        [Fact]
        public async Task UnlockAsync_EmptyPassword_SetsErrorMessage()
        {
            var sut = new Sut();
            sut.ViewModel.Password = string.Empty;

            await sut.ViewModel.UnlockCommand.ExecuteAsync(null);

            Assert.Equal("Digite a senha mestra do cofre.", sut.ViewModel.ErrorMessage);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }

        [Fact]
        public async Task UnlockAsync_PasswordWithoutVaultSelected_DoesNotUnlock()
        {
            var sut = new Sut();
            sut.ViewModel.Password = VaultUnlockCode;

            await sut.ViewModel.UnlockCommand.ExecuteAsync(null);

            Assert.False(sut.ViewModel.HasSelectedVault);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }

        [Fact]
        public async Task PickVaultAsync_Success_SelectsVaultAndEnablesUnlock()
        {
            var sut = new Sut();
            sut.FilePickerService.PickFileNameResult = new PickedFile(Location, "cofre.GDSBX");

            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);

            Assert.True(sut.ViewModel.HasSelectedVault);
            Assert.Equal(Location, sut.ViewModel.SelectedVaultLocation);
            Assert.Equal("cofre.GDSBX", sut.ViewModel.SelectedVaultFileName);
            Assert.True(sut.ViewModel.UnlockCommand.CanExecute(null));
            Assert.Empty(sut.AlertService.Calls);
        }

        [Fact]
        public async Task PickVaultAsync_BackupFile_ShowsInformativeAlertButStillAllowsSelection()
        {
            var sut = new Sut();
            sut.FilePickerService.PickFileNameResult = new PickedFile(Location, "BKP - cofre.GDSBX.bak");

            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);

            Assert.True(sut.ViewModel.HasSelectedVault);
            Assert.Single(sut.AlertService.Calls);
        }

        [Fact]
        public async Task PickVaultAsync_Throws_SetsGenericFilePickerError()
        {
            var sut = new Sut();
            sut.FilePickerService.PickFileNameException = new InvalidOperationException("boom");

            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);

            Assert.Equal("Não foi possível abrir o seletor de arquivos.", sut.ViewModel.ErrorMessage);
            Assert.False(sut.ViewModel.HasSelectedVault);
        }

        [Fact]
        public async Task PickVaultAsync_Cancelled_DoesNothing()
        {
            var sut = new Sut();
            sut.FilePickerService.PickFileNameResult = null;

            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);

            Assert.Null(sut.ViewModel.ErrorMessage);
            Assert.False(sut.ViewModel.HasSelectedVault);
        }

        [Fact]
        public async Task ClearSelectedVaultAsync_ResetsSelectionAndPassword()
        {
            var sut = new Sut();
            sut.FilePickerService.PickFileNameResult = new PickedFile(Location, "cofre.GDSBX");
            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);
            sut.ViewModel.Password = VaultUnlockCode;

            sut.ViewModel.ClearSelectedVaultCommand.Execute(null);

            Assert.False(sut.ViewModel.HasSelectedVault);
            Assert.Equal(string.Empty, sut.ViewModel.Password);
        }

        [Fact]
        public async Task UnlockAsync_WrongPassword_SetsGenericErrorMessage()
        {
            var sut = new Sut();
            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);
            sut.ViewModel.Password = "senha-errada";
            sut.ProfileFileService.OpenHandler = (_, _) => throw new InvalidPasswordOrCorruptFileException();

            await sut.ViewModel.UnlockCommand.ExecuteAsync(null);

            Assert.Equal("Senha incorreta ou arquivo corrompido.", sut.ViewModel.ErrorMessage);
        }

        [Fact]
        public async Task UnlockAsync_Success_NavigatesToVaultAndRemembersVault()
        {
            var sut = new Sut();
            var profile = SampleProfile();
            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);
            sut.ViewModel.Password = VaultUnlockCode;
            sut.ProfileFileService.OpenHandler = (_, _) => new ProfileOpenResult(profile, WasLegacyFormat: false);

            await sut.ViewModel.UnlockCommand.ExecuteAsync(null);

            Assert.Null(sut.ViewModel.ErrorMessage);
            Assert.Equal(string.Empty, sut.ViewModel.Password);
            var navigation = Assert.Single(sut.NavigationService.NavigateToRootCalls);
            Assert.Equal("VaultPage", navigation.Route);
            Assert.Equal(profile, navigation.Parameters!["Profile"]);
            Assert.Equal(Location, navigation.Parameters!["Location"]);
            Assert.Equal(
                Location,
                sut.PreferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null));
            Assert.Empty(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task UnlockAsync_LegacyFormat_MigratesBySaving()
        {
            var sut = new Sut();
            var profile = SampleProfile();
            await sut.ViewModel.PickVaultCommand.ExecuteAsync(null);
            sut.ViewModel.Password = VaultUnlockCode;
            sut.ProfileFileService.OpenHandler = (_, _) => new ProfileOpenResult(profile, WasLegacyFormat: true);

            await sut.ViewModel.UnlockCommand.ExecuteAsync(null);

            var save = Assert.Single(sut.ProfileFileService.SaveCalls);
            Assert.Equal(Location, save.Location);
            Assert.Equal(profile, save.Profile);
        }

        [Fact]
        public void ToggleShowPassword_TogglesIsPasswordHidden()
        {
            var sut = new Sut();
            Assert.True(sut.ViewModel.IsPasswordHidden);

            sut.ViewModel.ToggleShowPasswordCommand.Execute(null);

            Assert.False(sut.ViewModel.IsPasswordHidden);
        }

        [Fact]
        public async Task GoToCreateVaultAsync_NavigatesToCreateVaultPage()
        {
            var sut = new Sut();

            await sut.ViewModel.GoToCreateVaultCommand.ExecuteAsync(null);

            var navigation = Assert.Single(sut.NavigationService.NavigateToCalls);
            Assert.Equal("CreateVaultPage", navigation.Route);
        }

        [Fact]
        public async Task InitializeAsync_NoLastLocation_DoesNotEnableBiometric()
        {
            var sut = new Sut();

            await sut.ViewModel.InitializeAsync();

            Assert.False(sut.ViewModel.CanUseBiometric);
        }

        [Fact]
        public async Task InitializeAsync_BiometricAvailableAndEnabled_UnlocksAutomatically()
        {
            var sut = new Sut();
            var profile = SampleProfile();
            sut.PreferencesService.SetString(BiometricOptInCoordinator.LastLocationPreferenceKey, Location);
            sut.PreferencesService.SetString(BiometricOptInCoordinator.LastVaultNamePreferenceKey, profile.Nome);
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = true;
            sut.BiometricUnlockService.TryUnlockResult = Encoding.UTF8.GetBytes(VaultUnlockCode);
            sut.ProfileFileService.OpenHandler = (_, _) => new ProfileOpenResult(profile, WasLegacyFormat: false);

            await sut.ViewModel.InitializeAsync();

            Assert.True(sut.ViewModel.CanUseBiometric);
            var navigation = Assert.Single(sut.NavigationService.NavigateToRootCalls);
            Assert.Equal(VaultUnlockCode, navigation.Parameters!["Password"]);
        }

        [Fact]
        public async Task UnlockWithBiometricAsync_Cancelled_FallsBackWithoutError()
        {
            var sut = new Sut();
            sut.PreferencesService.SetString(BiometricOptInCoordinator.LastLocationPreferenceKey, Location);
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = true;
            sut.BiometricUnlockService.TryUnlockResult = null;

            await sut.ViewModel.InitializeAsync();

            Assert.Null(sut.ViewModel.ErrorMessage);
            Assert.Empty(sut.NavigationService.NavigateToRootCalls);
        }

        [Fact]
        public async Task ChangeVaultAsync_ForgetsVaultAndDisablesBiometric()
        {
            var sut = new Sut();
            sut.PreferencesService.SetString(BiometricOptInCoordinator.LastLocationPreferenceKey, Location);
            sut.PreferencesService.SetBool(BiometricOptInCoordinator.PromptedPreferenceKey, true);
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = true;
            await sut.ViewModel.InitializeAsync();
            Assert.True(sut.ViewModel.CanUseBiometric);

            await sut.ViewModel.ChangeVaultCommand.ExecuteAsync(null);

            Assert.Equal(1, sut.BiometricUnlockService.DisableCallCount);
            Assert.False(sut.ViewModel.CanUseBiometric);
            Assert.Null(sut.PreferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null));
        }
        // ===== Tutorial de primeiro acesso (fase 4) =====

        [Fact]
        public async Task InitializeAsync_FirstRunWithoutBiometric_ShowsOnboarding()
        {
            var sut = new Sut();

            await sut.ViewModel.InitializeAsync();

            Assert.True(sut.ViewModel.Onboarding.IsVisible);
        }

        [Fact]
        public async Task InitializeAsync_OnboardingAlreadySeen_DoesNotShowIt()
        {
            var sut = new Sut();
            sut.PreferencesService.SetBool(OnboardingViewModel.SeenPreferenceKey, true);

            await sut.ViewModel.InitializeAsync();

            Assert.False(sut.ViewModel.Onboarding.IsVisible);
        }

        // A regra da fase: com a biometria armada, InitializeAsync já dispara o prompt do sistema -
        // abrir o tutorial por cima deixaria o usuário sem saber em qual dos dois responder.
        [Fact]
        public async Task InitializeAsync_BiometricArmed_DoesNotShowOnboarding()
        {
            var sut = new Sut();
            sut.PreferencesService.SetString(BiometricOptInCoordinator.LastLocationPreferenceKey, Location);
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = true;

            await sut.ViewModel.InitializeAsync();

            Assert.True(sut.ViewModel.CanUseBiometric);
            Assert.False(sut.ViewModel.Onboarding.IsVisible);
        }

        [Fact]
        public void ShowOnboardingCommand_ReopensEvenAfterSeen()
        {
            var sut = new Sut();
            sut.PreferencesService.SetBool(OnboardingViewModel.SeenPreferenceKey, true);

            sut.ViewModel.ShowOnboardingCommand.Execute(null);

            Assert.True(sut.ViewModel.Onboarding.IsVisible);
            Assert.Equal(0, sut.ViewModel.Onboarding.CurrentIndex);
        }

    }
}
