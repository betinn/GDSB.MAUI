using CommunityToolkit.Mvvm.ComponentModel;
using GDSB.MAUI.Localization;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.ViewModels
{
    // ViewModel do dropdown de idioma na tela inicial. Aplica e grava a escolha no próprio evento
    // de mudança, sem botão de confirmar - ver decisão fechada no plano da rodada.
    public partial class LanguageSelectorViewModel : ObservableObject
    {
        private readonly ILocalizationService _localizationService;

        public LanguageSelectorViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            // Semeado via a propriedade gerada (Selected), não atribuído direto ao campo: sem
            // isso o app reabre no idioma certo mas o dropdown aparece em branco, porque
            // SelectedItem não bate com nenhum item de Options - o estado fica correto e a tela
            // mente sobre ele. Passar pela propriedade dispara OnSelectedChanged já na construção,
            // e é por isso que a guarda de igualdade ali não é opcional: sem ela, entraria em laço
            // reescrevendo a preferência que acabou de ser lida.
            Selected = _localizationService.Current;
        }

        // UnlockViewModel precisa deste serviço só para o próprio construtor passar pra base
        // LocalizedObject - ver "no mesmo padrão de Onboarding e BiometricOptIn" no plano da fase 1.
        public ILocalizationService LocalizationService => _localizationService;

        // Sonar sugere static porque o corpo não lê estado de instância - mas o binding de XAML
        // ("{Binding Language.Options}") exige uma propriedade de instância. Mesma justificativa
        // de S2325 usada em UnlockViewModel/OnboardingViewModel.
#pragma warning disable S2325
        public IReadOnlyList<AppLanguage> Options => AppLanguage.All;
#pragma warning restore S2325

        // Inicializado com o próprio default (não null!): sobrescrito no construtor pela leitura
        // real da preferência, mas evita o aviso de nulidade do campo não-anulável e deixa o tipo
        // sempre em um estado válido mesmo antes do construtor rodar até o fim.
        [ObservableProperty]
        private AppLanguage selected = AppLanguage.Default;

        partial void OnSelectedChanged(AppLanguage value)
        {
            if (value == _localizationService.Current)
                return;

            _localizationService.SetLanguage(value);
        }
    }
}
