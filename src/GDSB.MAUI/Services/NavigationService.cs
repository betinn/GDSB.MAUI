namespace GDSB.MAUI.Services
{
    public class NavigationService : INavigationService
    {
        public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null) =>
            parameters is null
                ? Shell.Current.GoToAsync(route)
                : Shell.Current.GoToAsync(route, parameters);

        public Task NavigateToRootAsync(string route, IDictionary<string, object>? parameters = null)
        {
            // "//" navega a partir da raiz do Shell: descarta qualquer página empilhada antes
            // (ex.: a tela de criar cofre) em vez de só empilhar mais uma por cima dela.
            var absoluteRoute = $"//{nameof(UnlockPage)}/{route}";
            return parameters is null
                ? Shell.Current.GoToAsync(absoluteRoute)
                : Shell.Current.GoToAsync(absoluteRoute, parameters);
        }

        public Task GoHomeAsync() => Shell.Current.GoToAsync($"//{nameof(UnlockPage)}");

        public Task GoBackAsync() => Shell.Current.GoToAsync("..");
    }
}
