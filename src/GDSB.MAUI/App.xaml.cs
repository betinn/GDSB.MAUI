using GDSB.MAUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GDSB.MAUI
{
    public partial class App : Application
    {
        private readonly IIdleLockService _idleLockService;

        public App(IServiceProvider serviceProvider, IIdleLockService idleLockService)
        {
            InitializeComponent();
            _idleLockService = idleLockService;
            MainPage = serviceProvider.GetRequiredService<AppShell>();
        }

        protected override void OnSleep() => _idleLockService.OnSleep();

        protected override async void OnResume() => await _idleLockService.OnResumeAsync();
    }
}
