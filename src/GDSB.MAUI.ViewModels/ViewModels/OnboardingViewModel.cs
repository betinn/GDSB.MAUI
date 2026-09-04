using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    public sealed record OnboardingSlide(string Title, string Body);

    /// <summary>
    /// Os três slides que o app mostra a quem nunca o viu. Aparecem sozinhos no primeiro acesso e
    /// continuam revisíveis pelo link "Como funciona?" no topo da tela de desbloqueio.
    ///
    /// O texto responde às três dúvidas que derrubam um usuário novo do GDSB, nesta ordem: onde
    /// meus dados ficam, como eu começo, e como eu volto depois. Nada de vocabulário técnico -
    /// "arquivo" e "senha", não "cofre criptografado" nem "derivação de chave".
    /// </summary>
    public partial class OnboardingViewModel : LocalizedObject
    {
        public const string SeenPreferenceKey = "gdsb.onboardingSeen";

        private readonly IPreferencesService _preferencesService;

        public OnboardingViewModel(IPreferencesService preferencesService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _preferencesService = preferencesService;
        }

        // Calculada sobre o catálogo (em vez de um inicializador de campo) porque este ViewModel
        // vive uma vez por app (ver UnlockOverlays) - reler a cada acesso é o que faz uma troca de
        // idioma valer aqui também, sem recriar nada. Só três slides: diferente de HelpTopics.All,
        // não vale a pena cachear por cultura.
        public IReadOnlyList<OnboardingSlide> Slides =>
        [
            new OnboardingSlide(Localization.Get("Onboarding_Slide1Title"), Localization.Get("Onboarding_Slide1Body")),
            new OnboardingSlide(Localization.Get("Onboarding_Slide2Title"), Localization.Get("Onboarding_Slide2Body")),
            new OnboardingSlide(Localization.Get("Onboarding_Slide3Title"), Localization.Get("Onboarding_Slide3Body")),
        ];

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentSlide))]
        [NotifyPropertyChangedFor(nameof(IsLastSlide))]
        [NotifyPropertyChangedFor(nameof(ShowSkip))]
        [NotifyPropertyChangedFor(nameof(AdvanceButtonText))]
        private int currentIndex;

        // Sonar não reconhece leitura de propriedade gerada por [ObservableProperty] como "dado de
        // instância" e sugere static - tornar estático quebraria o binding de XAML. Mesma
        // justificativa de UnlockViewModel.
#pragma warning disable S2325
        public OnboardingSlide CurrentSlide => Slides[CurrentIndex];

        public int SlideCount => Slides.Count;

        public bool IsLastSlide => CurrentIndex >= Slides.Count - 1;

        // No último slide o "Pular" some: a única saída passa a ser "Começar", que é a mesma coisa
        // sem parecer que o usuário está abandonando algo pela metade.
        public bool ShowSkip => !IsLastSlide;

        public string AdvanceButtonText => IsLastSlide ? Localization.Get("Onboarding_AdvanceButtonFinish") : Localization.Get("Onboarding_AdvanceButtonNext");
#pragma warning restore S2325

        public bool HasBeenSeen => _preferencesService.GetBool(SeenPreferenceKey, false);

        /// <summary>
        /// Abre o tutorial do zero. Usado pelo link "Como funciona?", que é o caminho de revisão -
        /// por isso não olha a preferência: quem pediu para rever quer rever.
        /// </summary>
        // S2325 é falso positivo: as duas atribuições são em propriedades geradas por
        // [ObservableProperty], que o Sonar não reconhece como estado de instância. Tornar o método
        // static quebraria o binding - e o UnlockViewModel o chama numa instância. Mesma
        // justificativa das propriedades de UnlockViewModel.
#pragma warning disable S2325
        public void ShowFromStart()
        {
            CurrentIndex = 0;
            IsVisible = true;
        }
#pragma warning restore S2325

        /// <summary>
        /// Abre sozinho, só uma vez na vida do app. Quem decide se é hora de chamar isto é o
        /// UnlockViewModel, que também tem a informação sobre a biometria.
        /// </summary>
        public void MaybeShowOnFirstRun()
        {
            // O !IsVisible importa: InitializeAsync roda de novo a cada Window.Resumed, e sem essa
            // guarda trocar de app no meio do slide 2 e voltar jogaria o usuário de volta no 1.
            if (!HasBeenSeen && !IsVisible)
                ShowFromStart();
        }

        [RelayCommand]
        private void Advance()
        {
            if (IsLastSlide)
            {
                Finish();
                return;
            }

            CurrentIndex++;
        }

        [RelayCommand]
        private void Skip() => Finish();

        // Pular conta como visto: quem fechou o tutorial não quer topar com ele de novo na próxima
        // abertura. O link "Como funciona?" continua ali para quando quiser.
        private void Finish()
        {
            _preferencesService.SetBool(SeenPreferenceKey, true);
            IsVisible = false;
            CurrentIndex = 0;
        }
    }
}
