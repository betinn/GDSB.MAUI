using GDSB.MAUI.Localization;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class LanguageSelectorViewModelTests
    {
        // O dropdown não pode nascer em branco: SelectedItem precisa bater com um item de Options
        // já na primeira renderização, mesmo quando o idioma salvo não é o default.
        [Fact]
        public void Constructor_SeedsSelectedFromCurrentLanguage()
        {
            var viewModel = new LanguageSelectorViewModel(new FakeLocalizationService(AppLanguage.En));

            Assert.Equal(AppLanguage.En, viewModel.Selected);
        }

        [Fact]
        public void Options_ListsAllLanguages()
        {
            var viewModel = new LanguageSelectorViewModel(new FakeLocalizationService());

            Assert.Equal(AppLanguage.All, viewModel.Options);
        }

        [Fact]
        public void ChangingSelected_AppliesToLocalizationService()
        {
            var localization = new FakeLocalizationService();
            var viewModel = new LanguageSelectorViewModel(localization);

            viewModel.Selected = AppLanguage.En;

            Assert.Equal(AppLanguage.En, localization.Current);
        }

        // Sem essa guarda, a semeadura do construtor (que passa pela propriedade, não pelo campo)
        // entraria em laço reaplicando o idioma que acabou de ser lido.
        [Fact]
        public void SettingSelectedToTheSameLanguage_DoesNotReapply()
        {
            var localization = new FakeLocalizationService();
            var viewModel = new LanguageSelectorViewModel(localization);
            var changeCount = 0;
            localization.LanguageChanged += (_, _) => changeCount++;

            viewModel.Selected = AppLanguage.PtBr;

            Assert.Equal(0, changeCount);
        }
    }
}
