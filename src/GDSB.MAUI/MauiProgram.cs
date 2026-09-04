using GDSB.Domain.Interfaces;
using GDSB.Infrastructure;
using GDSB.Infrastructure.Backup;
using GDSB.Infrastructure.Encryption.Legacy;
using GDSB.Infrastructure.Encryption.V2;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using GDSB.MAUI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

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

            RegisterServices(builder.Services);
            RegisterViewModels(builder.Services);
            RegisterPages(builder.Services);
            ConfigurePickerHandler(builder);

#if WINDOWS
            Platforms.Windows.CursorMappings.Apply();
#endif

            var app = builder.Build();

            // Precisa rodar aqui, antes do retorno: o host da plataforma constrói a App (e a
            // AppShell) sob demanda depois disso, e o TrExtension lê o catálogo na cultura vigente
            // no momento em que cada binding "{loc:Tr ...}" avalia pela primeira vez. Aplicada
            // depois, a primeira renderização sairia em português mesmo com inglês gravado. O
            // construtor de LocalizationService já aplica a cultura salva - só precisamos resolvê-lo
            // uma vez aqui.
            app.Services.GetRequiredService<ILocalizationService>();

            return app;
        }

        // Sem isto, o Picker do dropdown de idioma (UnlockPage) não cabe dentro da pílula: o
        // sublinhado do EditText nativo no Android e a borda do ComboBox no Windows sobram além do
        // Border que o envolve. É um ajuste global do handler porque hoje só existe este Picker no
        // app inteiro.
        private static void ConfigurePickerHandler(MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("GdsbPickerNoUnderline", (handler, _) =>
                {
                    handler.PlatformView.Background = null;
                });
#elif WINDOWS
                Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("GdsbPickerNoBorder", (handler, _) =>
                {
                    handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                });
#endif
            });
        }

        private static void RegisterServices(IServiceCollection services)
        {
#if ANDROID
            services.AddSingleton<IFilePickerService, Platforms.Android.Services.FilePickerService>();
            services.AddSingleton<IVaultFileSystem, Platforms.Android.Services.AndroidSafFileSystem>();
            services.AddSingleton<IBiometricUnlockService, Platforms.Android.Services.BiometricUnlockService>();
#elif WINDOWS
            services.AddSingleton<IFilePickerService, Platforms.Windows.Services.FilePickerService>();
            services.AddSingleton<IVaultFileSystem, LocalFileSystem>();
            services.AddSingleton<IBiometricUnlockService, Platforms.Windows.Services.BiometricUnlockService>();
#endif

#pragma warning disable CS0618 // leitor legado obsoleto, usado só por trás de IProfileFileService
            services.AddSingleton<IFileDecryptionService, LegacyV1FileDecryptionService>();
#pragma warning restore CS0618
            services.AddSingleton<IFileCryptoServiceV2, AesGcmFileCryptoService>();
            // FileSystem.AppDataDirectory funciona igual em Android e Windows - é isso que
            // equaliza o comportamento do backup nas duas plataformas (no Windows ele deixa de
            // cair dentro da pasta sincronizada do Drive/OneDrive).
            services.AddSingleton<IVaultBackupStore>(_ =>
                new FileSystemVaultBackupStore(Path.Combine(FileSystem.AppDataDirectory, "vault-backups")));
            services.AddSingleton<IProfileFileService, ProfileFileService>();

            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IAlertService, AlertService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IIdleLockService, IdleLockService>();
            services.AddSingleton<IPreferencesService, PreferencesService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IAppLauncherService, AppLauncherService>();
            services.AddSingleton<IVaultSessionService, VaultSessionService>();
            services.AddSingleton<VaultAccess>();
        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            // Transient (não Singleton): cada tela que oferece o opt-in (Unlock, CreateVault)
            // precisa da sua própria instância, com seu próprio TaskCompletionSource pendente.
            services.AddTransient<BiometricOptInCoordinator>();

            services.AddTransient<LanguageSelectorViewModel>();
            services.AddTransient<OnboardingViewModel>();
            services.AddTransient<UnlockOverlays>();
            services.AddTransient<UnlockViewModel>();
            services.AddTransient<VaultViewModel>();
            services.AddTransient<CreateVaultViewModel>();
            services.AddTransient<VaultSettingsViewModel>();
            services.AddTransient<BackupRecoveryViewModel>();
        }

        private static void RegisterPages(IServiceCollection services)
        {
            services.AddTransient<UnlockPage>();
            services.AddTransient<VaultPage>();
            services.AddTransient<CreateVaultPage>();
            services.AddTransient<VaultSettingsPage>();
            services.AddTransient<BackupRecoveryPage>();
            services.AddTransient<AppShell>();
        }
    }
}
