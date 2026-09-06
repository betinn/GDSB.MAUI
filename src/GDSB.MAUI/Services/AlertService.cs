namespace GDSB.MAUI.Services
{
    public class AlertService : IAlertService
    {
        public Task DisplayAlertAsync(string? title, string message, string cancel) =>
            Shell.Current?.DisplayAlertAsync(title, message, cancel) ?? Task.CompletedTask;
    }
}
