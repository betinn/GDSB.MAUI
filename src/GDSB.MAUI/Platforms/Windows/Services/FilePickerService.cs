using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GDSB.MAUI.Interfaces;
using Windows.Storage.Pickers;
using Windows.Storage;


namespace GDSB.MAUI.Platforms.Windows.Services
{
    public class FilePickerService : IFilePickerService
    {

        public async Task<string> PickFileNameAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".GDSBX");

            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            return file?.Path ?? string.Empty;
        }

        public async Task<string> PickSaveLocationAsync(string suggestedName)
        {
            var picker = new FileSavePicker();
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Arquivo GDSB", new List<string> { ".GDSBX" });

            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // StorageFile.Path aqui já é um caminho de arquivo real (inclusive dentro de uma pasta
            // sincronizada, tipo OneDrive) - ao contrário do Android, não precisa de nenhum
            // tratamento especial de URI.
            StorageFile file = await picker.PickSaveFileAsync();
            return file?.Path ?? string.Empty;
        }
    }
}
