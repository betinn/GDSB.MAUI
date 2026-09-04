using System.ComponentModel;
using System.Globalization;
using GDSB.MAUI.Localization;
using GDSB.MAUI.ViewModels.Resources;
using Microsoft.Maui.Controls;

// Mesma convenção de ILocalizationService: implementação de um serviço "do app" que mora neste
// projeto por precisar ser testável, mas fica no namespace que o resto do app já importa.
namespace GDSB.MAUI.Services
{
    // Singleton (ver MauiProgram.RegisterServices). Lê a preferência gravada no construtor, aplica
    // a cultura correspondente e passa a expor Current - é por isso que resolver este serviço uma
    // vez logo depois de builder.Build() (MauiProgram) já é suficiente para deixar a cultura
    // correta antes do primeiro XAML ser parseado.
    public sealed class LocalizationService : ILocalizationService, INotifyPropertyChanged
    {
        public const string LanguagePreferenceKey = "gdsb.language";

        private readonly IPreferencesService _preferencesService;

        // WeakEventManager em vez de um "event EventHandler? LanguageChanged" comum: os ViewModels
        // que assinam (via LocalizedObject) são Transient e este serviço é Singleton, então uma
        // assinatura forte manteria cada ViewModel descartado vivo na memória. Com referência fraca,
        // o assinante pode ser coletado mesmo sem chamar Dispose - que continua existindo em
        // LocalizedObject como defesa em profundidade, não como único mecanismo de limpeza.
        private readonly WeakEventManager _languageChangedEventManager = new();

        public LocalizationService(IPreferencesService preferencesService)
        {
            _preferencesService = preferencesService;

            var savedCode = _preferencesService.GetString(LanguagePreferenceKey, null);
            Current = AppLanguage.FromCode(savedCode);
            ApplyCulture(Current);
        }

        public AppLanguage Current { get; private set; }

        public event EventHandler? LanguageChanged
        {
            add => _languageChangedEventManager.AddEventHandler(value);
            remove => _languageChangedEventManager.RemoveEventHandler(value);
        }

        // O binding "{loc:Tr Chave}" do TrExtension liga direto neste indexador (Binding
        // "[Chave]") - é por isso que este tipo também implementa INotifyPropertyChanged e notifica
        // "Item[]" em SetLanguage, além do LanguageChanged de cima.
        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key] => Get(key);

        public string Get(string key) => AppStrings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        public string Format(string key, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, Get(key), args);

        public void SetLanguage(AppLanguage language)
        {
            if (language == Current)
                return;

            Current = language;
            _preferencesService.SetString(LanguagePreferenceKey, language.Code);
            ApplyCulture(language);

            // As duas notificações são necessárias: "Item[]" é a convenção do .NET para indexador
            // (o que o binding "[Chave]" observa) e null/string vazia é "tudo mudou" - cobre os
            // dois caminhos que o BindingExpression do MAUI pode ter tomado para avaliar o binding.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

            _languageChangedEventManager.HandleEvent(this, EventArgs.Empty, nameof(LanguageChanged));
        }

        // DefaultThreadCurrentCulture/UICulture não afetam a thread que já está rodando (só as
        // próximas criadas) - por isso também setamos CurrentCulture/CurrentUICulture da thread
        // atual, que é a de UI no MAUI.
        private static void ApplyCulture(AppLanguage language)
        {
            var culture = new CultureInfo(language.Code);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
