namespace GDSB.MAUI.Services
{
    public class AlertService : IAlertService
    {
        public Task DisplayAlertAsync(string? title, string message, string cancel) =>
            Shell.Current?.DisplayAlert(title, message, cancel) ?? Task.CompletedTask;
    }
}
