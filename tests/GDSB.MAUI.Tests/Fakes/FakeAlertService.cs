using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeAlertService : IAlertService
    {
        public List<(string? Title, string Message, string Cancel)> Calls { get; } = new();

        public Task DisplayAlertAsync(string? title, string message, string cancel)
        {
            Calls.Add((title, message, cancel));
            return Task.CompletedTask;
        }
    }
}
