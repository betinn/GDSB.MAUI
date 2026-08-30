using GDSB.Domain.Entities;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class SecretBoxItemViewModelTests
    {
        [Fact]
        public void Initial_ReturnsFirstLetterUppercase()
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { BoxName = "netflix" });

            Assert.Equal("N", vm.Initial);
        }

        [Fact]
        public void Initial_EmptyName_ReturnsQuestionMark()
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { BoxName = string.Empty });

            Assert.Equal("?", vm.Initial);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("obs", true)]
        public void HasObs_ReflectsBoxObs(string obs, bool expected)
        {
            var vm = new SecretBoxItemViewModel(new SecretBox { Obs = obs });

            Assert.Equal(expected, vm.HasObs);
        }
    }
}
