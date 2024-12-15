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
    }
}
