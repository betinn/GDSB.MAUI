using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeClipboardService : IClipboardService
    {
        public List<string> Calls { get; } = new();

        public Task SetTextAsync(string text)
        {
            Calls.Add(text);
            return Task.CompletedTask;
        }
    }
}
