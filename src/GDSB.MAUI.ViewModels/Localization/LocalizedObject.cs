using CommunityToolkit.Mvvm.ComponentModel;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.Localization
{
    // Base para qualquer ViewModel que expõe texto vindo do catálogo (AppStrings). Assina
    // LanguageChanged e reemite OnPropertyChanged(string.Empty) - "tudo mudou" - fazendo cada
    // propriedade calculada a partir do catálogo (ex.: UnlockButtonText) se reavaliar sozinha,
    // sem código por propriedade.
    //
    // A assinatura em si já é segura contra vazamento de memória: LocalizationService.LanguageChanged
    // usa WeakEventManager, e um ViewModel Transient descartado pode ser coletado mesmo sem Dispose
    // chamado. IDisposable aqui é defesa em profundidade, não o único mecanismo de limpeza.
    public abstract class LocalizedObject : ObservableObject, IDisposable
    {
        private readonly ILocalizationService _localizationService;
        private readonly EventHandler _onLanguageChanged;
        private bool _disposed;

        protected LocalizedObject(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _onLanguageChanged = (_, _) => OnPropertyChanged(string.Empty);
            _localizationService.LanguageChanged += _onLanguageChanged;
        }

        protected ILocalizationService Localization => _localizationService;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // protected virtual, não private: LocalizedObject é abstract, e o padrão de Dispose do
        // .NET exige o método sobrescrevível para uma subclasse que precise liberar recursos
        // próprios também poder participar da cadeia (mesmo que nenhuma subclasse atual precise).
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _localizationService.LanguageChanged -= _onLanguageChanged;

            _disposed = true;
        }
    }
}
