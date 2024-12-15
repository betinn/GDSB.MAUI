namespace GDSB.MAUI
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            var appShell = serviceProvider.GetService<AppShell>();
            MainPage = appShell;
        }
    }

}
