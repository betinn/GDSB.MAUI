using Android.App;
using Android.Content;
using GDSB.MAUI.Interfaces;
using Microsoft.Maui.ApplicationModel;

namespace GDSB.MAUI.Platforms.Android.Services
{
    // Usa o Storage Access Framework (SAF) direto, em vez do FilePicker do MAUI Essentials: o
    // FilePicker copia o conteúdo escolhido para o cache do app e devolve o caminho dessa cópia,
    // nunca o arquivo original - qualquer gravação depois disso nunca chega no arquivo de verdade,
    // o que quebra o uso com pastas sincronizadas (Google Drive, OneDrive). Com SAF, guardamos o
    // content:// URI com permissão persistente e lemos/gravamos nele direto via ContentResolver
    // (ver AndroidSafFileSystem) - inclusive quando o provedor por trás é um app de sync, que
    // então cuida de subir a mudança sozinho.
    public class FilePickerService : IFilePickerService
    {
        private const int RequestOpenDocument = 41001;
        private const int RequestCreateDocument = 41002;
        private const string GdsbMimeType = "application/octet-stream";

        public Task<string> PickFileNameAsync()
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { GdsbMimeType });
            return LaunchAndAwaitAsync(intent, RequestOpenDocument);
        }

        public Task<string> PickSaveLocationAsync(string suggestedName)
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(GdsbMimeType);
            intent.PutExtra(Intent.ExtraTitle, suggestedName);
            return LaunchAndAwaitAsync(intent, RequestCreateDocument);
        }

        private static Task<string> LaunchAndAwaitAsync(Intent intent, int requestCode)
        {
            var tcs = new TaskCompletionSource<string>();
            var activity = Platform.CurrentActivity;

            if (activity is null)
            {
                tcs.SetResult(null!);
                return tcs.Task;
            }

            MainActivity.RegisterDocumentPickCallback(requestCode, (resultCode, data) =>
            {
                var uri = resultCode == Result.Ok ? data?.Data : null;
                if (uri is null)
                {
                    tcs.TrySetResult(null!);
                    return;
                }

                try
                {
                    var takeFlags = data!.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    activity.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);
                }
                catch
                {
                    // Alguns provedores (ou versões antigas do Android) não suportam permissão
                    // persistente - a leitura/gravação ainda funciona normalmente nesta sessão.
                }

                tcs.TrySetResult(uri.ToString()!);
            });

            activity.StartActivityForResult(intent, requestCode);
            return tcs.Task;
        }
    }
}
