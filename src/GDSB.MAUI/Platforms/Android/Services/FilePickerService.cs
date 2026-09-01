using System.Linq;
using Android.App;
using Android.Content;
using Android.Provider;
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

        public async Task<PickedFile?> PickFileNameAsync()
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { GdsbMimeType });
            var location = await LaunchAndAwaitAsync(intent, RequestOpenDocument);

            if (string.IsNullOrEmpty(location))
                return null;

            return new PickedFile(location, GetDisplayName(location));
        }

        // OpenableColumns.DisplayName é o nome "de exibição" do documento, que pode não ter
        // relação nenhuma com o último segmento do content:// URI (provedores como o Google Drive
        // usam IDs opacos ali) - sem essa query não dá pra mostrar o nome escolhido na tela nem
        // reconhecer que o arquivo é um backup.
        private static string GetDisplayName(string location)
        {
            try
            {
                var resolver = Platform.CurrentActivity?.ContentResolver
                    ?? global::Android.App.Application.Context.ContentResolver;
                var uri = global::Android.Net.Uri.Parse(location);

                using var cursor = resolver?.Query(uri!, new[] { OpenableColumns.DisplayName }, null, null, null);
                if (cursor is not null && cursor.MoveToFirst())
                {
                    var columnIndex = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                    if (columnIndex >= 0)
                    {
                        var name = cursor.GetString(columnIndex);
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
            }
            catch
            {
                // Cai pro fallback abaixo - vale mais mostrar algo do que quebrar o fluxo de abrir
                // o cofre por causa de um nome de exibição que não veio.
            }

            return location.Split('/').LastOrDefault() ?? location;
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
