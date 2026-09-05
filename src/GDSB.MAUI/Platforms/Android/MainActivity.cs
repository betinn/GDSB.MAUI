using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

namespace GDSB.MAUI
{
    // WindowSoftInputMode fica aqui, no atributo, e não no AndroidManifest.xml: o manifesto escrito
    // à mão declara <activity android:name=".MainActivity">, mas o nome que o .NET Android gera para
    // esta classe é mangled (crc64...), então atributos colocados naquele nó não caem
    // necessariamente na Activity que o app realmente sobe. Pelo atributo o valor vai direto para a
    // entrada gerada. AdjustResize evita que o Android empurre a janela inteira (pan) quando o
    // teclado abre - o ajuste de fato é feito pelo SafeAreaEdges das páginas.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
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

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (PendingDocumentPicks.Remove(requestCode, out var callback))
                callback(resultCode, data);
        }
    }
}
