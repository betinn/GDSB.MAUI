using GDSB.MAUI.Interfaces;
using Microsoft.Extensions.Logging;

namespace GDSB.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            //builder.Services.AddSingleton<IFilePickerService, Platforms.Windows.Services.FilePickerService>();
#if ANDROID
            builder.Services.AddSingleton<IFilePickerService, Platforms.Android.Services.FilePickerService>();
#elif WINDOWS
            builder.Services.AddSingleton<IFilePickerService, Platforms.Windows.Services.FilePickerService>();
#endif
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<FileFinderDecrypter>();

            return builder.Build();
        }
    }
}
