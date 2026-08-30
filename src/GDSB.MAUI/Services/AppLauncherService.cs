namespace GDSB.MAUI.Services
{
    public class AppLauncherService : IAppLauncherService
    {
        public Task OpenAsync(Uri uri) => Launcher.Default.OpenAsync(uri);
    }
}
