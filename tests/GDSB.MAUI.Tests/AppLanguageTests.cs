using GDSB.MAUI.Localization;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class AppLanguageTests
    {
        // Caminho da primeira abertura: nenhuma preferência gravada ainda.
        [Fact]
        public void FromCode_Null_ReturnsDefault()
        {
            Assert.Equal(AppLanguage.PtBr, AppLanguage.FromCode(null));
        }

        // Caminho de uma preferência gravada por uma versão futura, com um idioma que esta versão
        // não conhece.
        [Fact]
        public void FromCode_UnknownCode_ReturnsDefault()
        {
            Assert.Equal(AppLanguage.PtBr, AppLanguage.FromCode("de"));
        }

        [Fact]
        public void FromCode_KnownCode_ReturnsMatchingLanguage()
        {
            Assert.Equal(AppLanguage.En, AppLanguage.FromCode("en-US"));
        }

        [Fact]
        public void Default_IsPtBr()
        {
            Assert.Equal(AppLanguage.PtBr, AppLanguage.Default);
        }

        [Fact]
        public void All_ContainsBothLanguages()
        {
            Assert.Equal(new[] { AppLanguage.PtBr, AppLanguage.En }, AppLanguage.All);
        }
    }
}
