using GDSB.MAUI.Services;

namespace GDSB.MAUI.Localization
{
    // Resolve ILocalizationService fora do container de DI, para os poucos pontos que não passam
    // por ele: TrExtension (markup extension, instanciado pelo parser XAML) e os controles que o
    // XAML cria direto (HelpButton, FieldLabelView) - todos sem construtor chamado pela DI. Página e
    // ViewModel recebem o serviço por injeção normal; só isto aqui precisa do atalho.
    internal static class LocalizationServiceLocator
    {
        // "?? throw" em vez de "!": prova a não-nulidade pro compilador sem um null-forgiving
        // operator (que o SonarCloud aponta como redundante aqui, apesar do compilador continuar
        // exigindo a checagem - IPlatformApplication.Current é anulável de verdade) e falha com uma
        // mensagem clara em vez de NullReferenceException, no caso extremo de isto rodar antes do
        // host da plataforma estar pronto.
        public static ILocalizationService Resolve()
        {
            var platformApplication = IPlatformApplication.Current
                ?? throw new InvalidOperationException("IPlatformApplication.Current não está definido.");

            return platformApplication.Services.GetRequiredService<ILocalizationService>();
        }
    }
}
