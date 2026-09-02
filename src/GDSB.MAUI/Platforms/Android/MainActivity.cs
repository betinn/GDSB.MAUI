using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace GDSB.MAUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestStorageId = 0;

        // Ponte pro Storage Access Framework (ver Platforms/Android/Services/FilePickerService):
        // o resultado de ActionOpenDocument/ActionCreateDocument só chega aqui, na Activity que
        // disparou o intent - o serviço registra um callback por request code e espera por ele.
        private static readonly Dictionary<int, Action<Result, Intent?>> PendingDocumentPicks = new();

        public static void RegisterDocumentPickCallback(int requestCode, Action<Result, Intent?> callback) =>
            PendingDocumentPicks[requestCode] = callback;

        // Ponte pro BiometricUnlockService: chamar BiometricPrompt.Authenticate antes da janela
        // ganhar foco (ex.: logo no OnCreate/primeiro OnResume, ou disparado de código nosso
        // assim que a página aparece) faz o prompt do sistema falhar silenciosamente ou nem
        // aparecer - o serviço espera esse evento com foco=true antes de chamar Authenticate.
        public static event Action<bool>? WindowFocusChanged;

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            WindowFocusChanged?.Invoke(hasFocus);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestStoragePermissions();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (PendingDocumentPicks.Remove(requestCode, out var callback))
                callback(resultCode, data);
        }

        void RequestStoragePermissions()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[]
                {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage
                }, RequestStorageId);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestStorageId)
            {
                var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
                System.Diagnostics.Debug.WriteLine(granted
                    ? "GDSB: legacy storage permission granted."
                    : "GDSB: legacy storage permission denied; file access relies on the Storage Access Framework picker instead.");
            }
        }
    }
}
