namespace GDSB.MAUI.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);

        // Reseta a pilha de navegação até a tela de desbloqueio antes de ir pra "route" - usado ao
        // entrar de vez num cofre (abrir ou criar), pra "voltar" nunca cair numa tela intermediária
        // já concluída (ex.: a de criar cofre depois que ele já foi criado), sempre na home zerada.
        Task NavigateToRootAsync(string route, IDictionary<string, object>? parameters = null);

        // Volta direto pra tela de desbloqueio, descartando toda a pilha. Usado pelo botão de
        // voltar do cofre: o pop relativo padrão do Shell ("..") não é confiável ali (ver
        // VaultPage.xaml/Shell.BackButtonBehavior) - isso sempre cai na home zerada.
        Task GoHomeAsync();

        Task GoBackAsync();
    }
}
