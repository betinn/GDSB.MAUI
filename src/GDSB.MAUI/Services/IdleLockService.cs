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
        public static readonly TimeSpan LockAfter = TimeSpan.FromMinutes(2);

        private readonly INavigationService _navigationService;
        private DateTime? _sleptAtUtc;

        public IdleLockService(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void OnSleep() => _sleptAtUtc = DateTime.UtcNow;

        public async Task OnResumeAsync()
        {
            if (_sleptAtUtc is not { } sleptAt)
                return;

            _sleptAtUtc = null;

            if (DateTime.UtcNow - sleptAt >= LockAfter)
                await _navigationService.GoHomeAsync();
        }
    }
}
