namespace GDSB.MAUI.Services
{
    public interface IAlertService
    {
        Task DisplayAlertAsync(string? title, string message, string cancel);
    }
}
