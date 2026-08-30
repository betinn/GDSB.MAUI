using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class BiometricOptInCoordinatorTests
    {
        private sealed class Sut
        {
            public FakeBiometricUnlockService BiometricUnlockService { get; } = new();
            public FakeAlertService AlertService { get; } = new();
            public FakePreferencesService PreferencesService { get; } = new();
            public BiometricOptInCoordinator Coordinator { get; }

            public Sut()
            {
                Coordinator = new BiometricOptInCoordinator(BiometricUnlockService, AlertService, PreferencesService);
            }
        }

        [Fact]
        public void RememberVault_StoresLocationAndName()
        {
            var sut = new Sut();

            sut.Coordinator.RememberVault("content://vault", "Meu Cofre");

            Assert.Equal("content://vault", sut.PreferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null));
            Assert.Equal("Meu Cofre", sut.PreferencesService.GetString(BiometricOptInCoordinator.LastVaultNamePreferenceKey, null));
        }

        [Fact]
        public void ForgetVault_RemovesEverything()
        {
            var sut = new Sut();
            sut.Coordinator.RememberVault("content://vault", "Meu Cofre");
            sut.PreferencesService.SetBool(BiometricOptInCoordinator.PromptedPreferenceKey, true);

            sut.Coordinator.ForgetVault();

            Assert.Null(sut.PreferencesService.GetString(BiometricOptInCoordinator.LastLocationPreferenceKey, null));
            Assert.Null(sut.PreferencesService.GetString(BiometricOptInCoordinator.LastVaultNamePreferenceKey, null));
            Assert.False(sut.PreferencesService.GetBool(BiometricOptInCoordinator.PromptedPreferenceKey, false));
        }

        [Fact]
        public async Task MaybeOfferAsync_AlreadyPrompted_DoesNothing()
        {
            var sut = new Sut();
            sut.PreferencesService.SetBool(BiometricOptInCoordinator.PromptedPreferenceKey, true);

            await sut.Coordinator.MaybeOfferAsync("senha");

            Assert.False(sut.Coordinator.IsVisible);
        }

        [Fact]
        public async Task MaybeOfferAsync_BiometricUnavailable_DoesNothing()
        {
            var sut = new Sut();
            sut.BiometricUnlockService.IsAvailable = false;

            await sut.Coordinator.MaybeOfferAsync("senha");

            Assert.False(sut.Coordinator.IsVisible);
            Assert.False(sut.PreferencesService.GetBool(BiometricOptInCoordinator.PromptedPreferenceKey, false));
        }

        [Fact]
        public async Task MaybeOfferAsync_Accepted_StoresKey()
        {
            var sut = new Sut();
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = false;
            sut.BiometricUnlockService.StoreKeyResult = true;

            var offerTask = sut.Coordinator.MaybeOfferAsync("senha");
            Assert.True(sut.Coordinator.IsVisible);

            sut.Coordinator.AcceptCommand.Execute(null);
            await offerTask;

            Assert.False(sut.Coordinator.IsVisible);
            Assert.True(sut.PreferencesService.GetBool(BiometricOptInCoordinator.PromptedPreferenceKey, false));
        }

        [Fact]
        public async Task MaybeOfferAsync_Declined_DoesNotStoreKey()
        {
            var sut = new Sut();
            sut.BiometricUnlockService.IsAvailable = true;
            sut.BiometricUnlockService.IsEnabled = false;

            var offerTask = sut.Coordinator.MaybeOfferAsync("senha");
            sut.Coordinator.DeclineCommand.Execute(null);
            await offerTask;

            Assert.False(sut.Coordinator.IsVisible);
        }
    }
}
