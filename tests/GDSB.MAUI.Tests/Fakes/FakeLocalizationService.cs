using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    // Não mexe em CultureInfo (diferente de LocalizationService de verdade) - é o que evita
    // vazamento de estado global de cultura entre testes que nem são sobre localização. Os testes
    // que precisam verificar a aplicação de cultura/persistência de verdade usam
    // LocalizationService diretamente com FakePreferencesService (ver LocalizationServiceTests).
    internal sealed class FakeLocalizationService : ILocalizationService
    {
        public FakeLocalizationService(AppLanguage? initial = null)
        {
            Current = initial ?? AppLanguage.Default;
        }

        public AppLanguage Current { get; private set; }

        public event EventHandler? LanguageChanged;

        public string this[string key] => Get(key);

        public string Get(string key) => key;

        public string Format(string key, params object[] args) => string.Format(key, args);

        public void SetLanguage(AppLanguage language)
        {
            if (language == Current)
                return;

            Current = language;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
