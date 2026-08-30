namespace GDSB.MAUI.Services
{
    // Encapsula Microsoft.Maui.ApplicationModel.Launcher (usado por VaultViewModel.OpenUrlAsync
    // para abrir a URL de um item) - mesmo motivo de IPreferencesService: testabilidade.
    public interface IAppLauncherService
    {
        Task OpenAsync(Uri uri);
    }
}
