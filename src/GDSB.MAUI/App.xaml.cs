using GDSB.MAUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GDSB.MAUI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IIdleLockService _idleLockService;

        public App(IServiceProvider serviceProvider, IIdleLockService idleLockService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _idleLockService = idleLockService;
        }

        // A raiz da janela é a AppShell resolvida do container (registrada como transiente em
        // MauiProgram), e não uma atribuição a MainPage - que o MAUI 10 depreciou. CreateWindow
        // roda depois do construtor da App, ou seja, ainda mais tarde que o ponto onde MauiProgram
        // aplica a cultura salva: quando o primeiro XAML é lido, o catálogo já está na cultura
        // certa. Devolvemos a Window pronta em vez de chamar base.CreateWindow, porque a
        // implementação da base só sabe criar janela a partir do MainPage obsoleto.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = _serviceProvider.GetRequiredService<AppShell>();
            return new Window(shell);
        }

        protected override void OnSleep() => _idleLockService.OnSleep();

        protected override async void OnResume() => await _idleLockService.OnResumeAsync();
    }
}
