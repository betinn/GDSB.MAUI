namespace GDSB.MAUI.Services
{
    public class ClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text) => Clipboard.SetTextAsync(text);
    }
}
