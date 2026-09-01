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

#if WINDOWS
            Platforms.Windows.CursorMappings.Apply();
#endif

            return builder.Build();
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
            services.AddSingleton<IAppLauncherService, AppLauncherService>();
            services.AddSingleton<IVaultSessionService, VaultSessionService>();
            services.AddSingleton<VaultAccess>();
        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            // Transient (não Singleton): cada tela que oferece o opt-in (Unlock, CreateVault)
            // precisa da sua própria instância, com seu próprio TaskCompletionSource pendente.
            services.AddTransient<BiometricOptInCoordinator>();

            services.AddTransient<UnlockViewModel>();
            services.AddTransient<VaultViewModel>();
            services.AddTransient<CreateVaultViewModel>();
            services.AddTransient<VaultSettingsViewModel>();
        }

        private static void RegisterPages(IServiceCollection services)
        {
            services.AddTransient<UnlockPage>();
            services.AddTransient<VaultPage>();
            services.AddTransient<CreateVaultPage>();
            services.AddTransient<VaultSettingsPage>();
            services.AddTransient<AppShell>();
        }
    }
}
