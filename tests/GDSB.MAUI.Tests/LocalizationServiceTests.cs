using System.ComponentModel;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;
using GDSB.MAUI.Tests.Fakes;
using Xunit;

namespace GDSB.MAUI.Tests
{
    // Usa a LocalizationService de verdade (não o fake) com FakePreferencesService - é o "fechar e
    // abrir o app" da fase 1 em forma de teste, sem depender do roteiro manual em aparelho.
    public class LocalizationServiceTests
    {
        [Fact]
        public void Constructor_NoSavedPreference_DefaultsToPtBr()
        {
            var service = new LocalizationService(new FakePreferencesService());

            Assert.Equal(AppLanguage.PtBr, service.Current);
        }

        [Fact]
        public void SetLanguage_PersistsAcrossANewInstanceOverTheSamePreferences()
        {
            var preferences = new FakePreferencesService();
            var first = new LocalizationService(preferences);

            first.SetLanguage(AppLanguage.En);
            var second = new LocalizationService(preferences);

            Assert.Equal(AppLanguage.En, second.Current);
        }

        [Fact]
        public void SetLanguage_SameLanguage_DoesNotRaiseLanguageChanged()
        {
            var service = new LocalizationService(new FakePreferencesService());
            var raised = false;
            service.LanguageChanged += (_, _) => raised = true;

            service.SetLanguage(AppLanguage.PtBr);

            Assert.False(raised);
        }

        // O binding "{loc:Tr Chave}" liga no indexador via Binding "[Chave]" - a convenção do .NET
        // pro BindingExpression reagir é o PropertyChanged("Item[]"). O null cobre o outro caminho
        // ("tudo mudou"). Faltar qualquer um dos dois é o jeito da troca ao vivo quebrar em
        // silêncio, só visível rodando o app - por isso este teste, não só o roteiro manual.
        [Fact]
        public void SetLanguage_NotifiesIndexerAndWildcardPropertyChanged()
        {
            var service = new LocalizationService(new FakePreferencesService());
            var propertyNames = new List<string?>();
            ((INotifyPropertyChanged)service).PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

            service.SetLanguage(AppLanguage.En);

            Assert.Contains("Item[]", propertyNames);
            Assert.Contains(null, propertyNames);
        }

        [Fact]
        public void Get_FollowsCurrentLanguage()
        {
            var service = new LocalizationService(new FakePreferencesService());

            var ptText = service.Get("Unlock_HowItWorks");
            service.SetLanguage(AppLanguage.En);
            var enText = service.Get("Unlock_HowItWorks");

            Assert.Equal("Como funciona?", ptText);
            Assert.Equal("How it works?", enText);
        }

        [Fact]
        public void Indexer_MatchesGet()
        {
            var service = new LocalizationService(new FakePreferencesService());

            Assert.Equal(service.Get("Unlock_Tagline"), service["Unlock_Tagline"]);
        }
    }
}
