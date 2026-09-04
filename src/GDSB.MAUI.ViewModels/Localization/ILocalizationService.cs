using GDSB.MAUI.Localization;

// Fica no namespace GDSB.MAUI.Services, e não GDSB.MAUI.Localization (a pasta em que o arquivo
// mora): é a mesma convenção de IPreferencesService/IAlertService, que também moram fisicamente
// neste projeto mas ficam no namespace que o resto do app já importa - os ViewModels que vão
// consumir este serviço já têm "using GDSB.MAUI.Services;".
namespace GDSB.MAUI.Services
{
    public interface ILocalizationService
    {
        AppLanguage Current { get; }

        // Assinado por LocalizedObject para reconstruir propriedades calculadas (ex.:
        // UnlockButtonText) que dependem do catálogo. Binding direto no XAML ("{loc:Tr Chave}")
        // não usa este evento - depende de LocalizationService implementar INotifyPropertyChanged
        // e notificar o indexador (ver TrExtension e o "ponto de risco" no plano da fase 1).
        event EventHandler? LanguageChanged;

        // É este indexador que o TrExtension liga via Binding "[Chave]" - por isso a classe
        // concreta também precisa implementar INotifyPropertyChanged e notificar "Item[]".
        string this[string key] { get; }

        string Get(string key);

        // Placeholder posicional no .resx ({0}, {1}...) - a ordem das palavras muda entre idiomas.
        string Format(string key, params object[] args);

        void SetLanguage(AppLanguage language);
    }
}
