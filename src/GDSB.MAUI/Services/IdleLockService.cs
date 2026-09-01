namespace GDSB.MAUI.Services
{
    // Auto-lock por inatividade: o app não tem como saber quanto tempo ficou em background
    // enquanto estava lá (o processo pode nem continuar rodando), só o intervalo entre OnSleep e
    // OnResume - por isso o relógio é medido nesse intervalo, não por um timer correndo o tempo
    // todo. Ultrapassado o limite, força a navegação de volta pro Unlock: como VaultPage/
    // CreateVaultPage são páginas transient (MauiProgram.RegisterPages), isso já derruba a
    // instância anterior do ViewModel (e o Profile/senha que ela segurava) em vez de só escondê-la.
    public class IdleLockService : IIdleLockService
    {
        private readonly INavigationService _navigationService;
        private readonly IVaultSessionService _vaultSessionService;
        private DateTime? _sleptAtUtc;

        public IdleLockService(INavigationService navigationService, IVaultSessionService vaultSessionService)
        {
            _navigationService = navigationService;
            _vaultSessionService = vaultSessionService;
        }

        public void OnSleep() => _sleptAtUtc = DateTime.UtcNow;

        public async Task OnResumeAsync()
        {
            if (_sleptAtUtc is not { } sleptAt)
                return;

            _sleptAtUtc = null;

            var settings = _vaultSessionService.Settings;
            if (!settings.AutoLockEnabled)
                return;

            if (DateTime.UtcNow - sleptAt >= TimeSpan.FromMinutes(settings.AutoLockMinutes))
            {
                _vaultSessionService.Clear();
                await _navigationService.GoHomeAsync();
            }
        }
    }
}
