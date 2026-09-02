namespace GDSB.MAUI.Services
{
    public class ClipboardService : IClipboardService
    {
        private readonly IVaultSessionService _vaultSessionService;

        private CancellationTokenSource? _pendingClear;

        public ClipboardService(IVaultSessionService vaultSessionService)
        {
            _vaultSessionService = vaultSessionService;
        }

        public async Task SetTextAsync(string text)
        {
            if (_pendingClear is not null)
                await _pendingClear.CancelAsync();

            await Clipboard.SetTextAsync(text);

            var settings = _vaultSessionService.Settings;
            if (!settings.ClipboardClearEnabled)
                return;

            var cts = new CancellationTokenSource();
            _pendingClear = cts;
            ScheduleClear(text, TimeSpan.FromSeconds(settings.ClipboardClearSeconds), cts.Token);
        }

        // Dispara sem esperar - SetTextAsync não deve ficar pendurada pra retornar. Só limpa se o
        // clipboard ainda tiver exatamente o valor copiado aqui: se o usuário copiou outra coisa
        // (dentro ou fora do app) nesse meio-tempo, limpar destruiria esse valor novo.
        private static void ScheduleClear(string copiedText, TimeSpan clearAfter, CancellationToken token) =>
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(clearAfter, token);
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
