using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class OnboardingViewModel : ObservableObject
    {
        public const string SeenPreferenceKey = "gdsb.onboardingSeen";

        private readonly IPreferencesService _preferencesService;

        public OnboardingViewModel(IPreferencesService preferencesService)
        {
            _preferencesService = preferencesService;
        }

        public IReadOnlyList<OnboardingSlide> Slides { get; } =
        [
            new OnboardingSlide(
                "Seu cofre é um arquivo",
                "O GDSB não guarda nada na nuvem nem dentro do celular: tudo mora num arquivo .GDSBX " +
                "que é seu. O app é só a camada que abre e lê esse arquivo - sem ele disponível, não há " +
                "o que abrir. Guarde-o onde você consiga alcançar de novo: uma pasta do Google Drive ou " +
                "do OneDrive, por exemplo."),

            new OnboardingSlide(
                "Criar um cofre",
                "Você escolhe onde salvar o arquivo e define a senha mestra. Essa senha é a única " +
                "chave: ela não fica guardada em lugar nenhum e não existe \"esqueci minha senha\". " +
                "Perdeu a senha, perdeu o conteúdo do arquivo."),

            new OnboardingSlide(
                "Abrir depois",
                "Sempre igual: escolher o arquivo, digitar a senha. Ligando a biometria, o app lembra " +
                "do último cofre e a digital vira o atalho - mas a senha mestra continua valendo, e o " +
                "arquivo ainda precisa estar acessível."),
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

        public string AdvanceButtonText => IsLastSlide ? "Começar" : "Próximo";
#pragma warning restore S2325

        public bool HasBeenSeen => _preferencesService.GetBool(SeenPreferenceKey, false);

        /// <summary>
        /// Abre o tutorial do zero. Usado pelo link "Como funciona?", que é o caminho de revisão -
        /// por isso não olha a preferência: quem pediu para rever quer rever.
        /// </summary>
        public void ShowFromStart()
        {
            CurrentIndex = 0;
            IsVisible = true;
        }

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
