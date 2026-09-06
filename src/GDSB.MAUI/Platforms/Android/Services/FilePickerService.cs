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

        // IOpenableColumns.DisplayName é o nome "de exibição" do documento, que pode não ter
        // relação nenhuma com o último segmento do content:// URI (provedores como o Google Drive
        // usam IDs opacos ali) - sem essa query não dá pra mostrar o nome escolhido na tela nem
        // reconhecer que o arquivo é um backup.
        private static string GetDisplayName(string location)
        {
            try
            {
                var resolver = Platform.CurrentActivity?.ContentResolver
                    ?? global::Android.App.Application.Context.ContentResolver;

                // Uri.Parse devolve null quando a location não é um URI válido - sem URI não há o
                // que perguntar ao provedor, então cai direto no fallback do fim do método.
                var uri = global::Android.Net.Uri.Parse(location);

                // A classe OpenableColumns foi depreciada no binding do Android em favor da
                // interface IOpenableColumns. O nome da coluna é anulável ali, e sem ele não há
                // projeção para consultar - cai no fallback junto com o URI inválido.
                var displayNameColumn = IOpenableColumns.DisplayName;

                using var cursor = uri is null || displayNameColumn is null
                    ? null
                    : resolver?.Query(uri, new[] { displayNameColumn }, null, null, null);
                if (cursor is not null && cursor.MoveToFirst())
                {
                    var columnIndex = cursor.GetColumnIndex(displayNameColumn);
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

            // Cadeia vazia é o "não escolheu nada" que os chamadores já tratam com
            // string.IsNullOrEmpty (PickFileNameAsync aqui e os ViewModels do outro lado).
            if (activity is null)
            {
                tcs.SetResult(string.Empty);
                return tcs.Task;
            }

            MainActivity.RegisterDocumentPickCallback(requestCode, (resultCode, data) =>
            {
                var uri = resultCode == Result.Ok ? data?.Data : null;
                if (data is null || uri is null)
                {
                    tcs.TrySetResult(string.Empty);
                    return;
                }

                try
                {
                    var takeFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    activity.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);
                }
                catch
                {
                    // Alguns provedores (ou versões antigas do Android) não suportam permissão
                    // persistente - a leitura/gravação ainda funciona normalmente nesta sessão.
                }

                // Uri.ToString() é anulável no binding (herda de Object.toString): sem texto não
                // há location para devolver, e cadeia vazia é o mesmo "não escolheu nada" tratado
                // acima, que os chamadores já leem com string.IsNullOrEmpty.
                tcs.TrySetResult(uri.ToString() ?? string.Empty);
            });

            activity.StartActivityForResult(intent, requestCode);
            return tcs.Task;
        }
    }
}
