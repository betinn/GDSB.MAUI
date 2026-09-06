using GDSB.Domain.Entities;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class SecretBoxItemViewModelTests
    {
        private static VaultViewModel CreateOwner() => new(
            new FakeClipboardService(),
            new FakeAlertService(),
            new FakeProfileFileService(),
            new FakeNavigationService(),
            new FakeAppLauncherService(),
            new FakeVaultSessionService(),
            new FakeLocalizationService());

        [Fact]
        public void Initial_ReturnsFirstLetterUppercase()
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { BoxName = "netflix" }, CreateOwner());

            Assert.Equal("N", vm.Initial);
        }

        [Fact]
        public void Initial_EmptyName_ReturnsQuestionMark()
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { BoxName = string.Empty }, CreateOwner());

            Assert.Equal("?", vm.Initial);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("obs", true)]
        public void HasObs_ReflectsBoxObs(string obs, bool expected)
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { Obs = obs }, CreateOwner());

            Assert.Equal(expected, vm.HasObs);
        }
    }
}
