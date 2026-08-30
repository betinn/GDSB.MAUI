using Microsoft.Extensions.DependencyInjection;

namespace GDSB.MAUI
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            MainPage = serviceProvider.GetRequiredService<AppShell>();
        }
    }
}
