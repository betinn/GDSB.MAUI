using GDSB.Domain.Interfaces;
using GDSB.Infrastructure.Encryption;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;

namespace GDSB.MAUI
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public App()
        {
            InitializeComponent();


            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            MainPage = new NavigationPage(new AppShell());
        }
        private void ConfigureServices(IServiceCollection services)
        {

#if ANDROID
            services.AddSingleton<IFilePickerService, Platforms.Android.Services.FilePickerService>();
#elif WINDOWS
            services.AddSingleton<IFilePickerService, Platforms.Windows.Services.FilePickerService>();
#endif
            services.AddSingleton<IFileDataService, FileDataService>();
            services.AddSingleton<IFileDecryptionService, FileDecryptionService>();
            
            // Adicione outras dependências aqui
        }
    }

}
