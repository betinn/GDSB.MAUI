using GDSB.MAUI.Services;

namespace GDSB.MAUI.Localization
{
    // Ponte entre "{loc:Tr Chave}" no XAML e o catálogo. Devolve um BindingBase (não uma string
    // resolvida na hora) - é isso que dá a troca de idioma ao vivo: o binding fica vivo, ligado ao
    // indexador de ILocalizationService, e reavalia sozinho quando o serviço notifica (ver
    // LocalizationService.SetLanguage e o "ponto de risco" documentado no plano da fase 1).
    [ContentProperty(nameof(Key))]
    public sealed class TrExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        // Só precisa ser setado explicitamente dentro de um DataTemplate (ex.:
        // Resources/HelpVisuals.xaml, fase 2): lá o BindingContext do binding avaliado é o item do
        // template, não o singleton de localização. Fora de um DataTemplate, o padrão (null) já
        // resolve o serviço certo.
        public object? Source { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var localizationService = Source ?? ResolveLocalizationServiceFromPlatformApplication();

            return new Binding($"[{Key}]", BindingMode.OneWay, source: localizationService);
        }

        // "?? throw" em vez de "!": prova a não-nulidade pro compilador sem um null-forgiving
        // operator (que o SonarCloud aponta como redundante aqui, apesar do compilador continuar
        // exigindo a checagem - IPlatformApplication.Current é anulável de verdade) e falha com uma
        // mensagem clara em vez de NullReferenceException, no caso extremo de o markup extension
        // avaliar antes do host da plataforma estar pronto. Em método próprio pra continuar só
        // sendo chamado quando Source não foi setado, preservando o curto-circuito do "??" de cima.
        private static object ResolveLocalizationServiceFromPlatformApplication()
        {
            var platformApplication = IPlatformApplication.Current
                ?? throw new InvalidOperationException("IPlatformApplication.Current não está definido.");

            return platformApplication.Services.GetRequiredService<ILocalizationService>();
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }
}
