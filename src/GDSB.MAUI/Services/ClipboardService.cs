namespace GDSB.MAUI.Services
{
    public class ClipboardService : IClipboardService
    {
        private static readonly TimeSpan ClearAfter = TimeSpan.FromSeconds(20);

        private CancellationTokenSource? _pendingClear;

        public async Task SetTextAsync(string text)
        {
            _pendingClear?.Cancel();
            var cts = new CancellationTokenSource();
            _pendingClear = cts;

            await Clipboard.SetTextAsync(text);
            ScheduleClear(text, cts.Token);
        }

        // Dispara sem esperar - SetTextAsync não deve ficar pendurada 20s pra retornar. Só limpa
        // se o clipboard ainda tiver exatamente o valor copiado aqui: se o usuário copiou outra
        // coisa (dentro ou fora do app) nesse meio-tempo, limpar destruiria esse valor novo.
        private static void ScheduleClear(string copiedText, CancellationToken token) =>
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ClearAfter, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                var current = await Clipboard.GetTextAsync();
                if (current == copiedText)
                    await Clipboard.SetTextAsync(string.Empty);
            }, CancellationToken.None);
    }
}
