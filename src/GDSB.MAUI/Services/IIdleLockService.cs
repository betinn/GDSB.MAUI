namespace GDSB.MAUI.Services
{
    // Ganchos de ciclo de vida do app (OnSleep/OnResume em App.xaml.cs), isolados numa interface
    // pelo mesmo motivo de IClipboardService/IAlertService/INavigationService: testabilidade.
    public interface IIdleLockService
    {
        void OnSleep();

        Task OnResumeAsync();
    }
}
