using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;
using GDSB.MAUI.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using static Android.Preferences.PreferenceManager;

namespace GDSB.MAUI.Platforms.Android.Services
{
    public class FilePickerService : IFilePickerService
    {
        private TaskCompletionSource<string> _taskCompletionSource;

        public async Task<string> PickFileNameAsync()
        {

            var result = await FilePicker.Default.PickAsync(new PickOptions()
            {
                PickerTitle = "Pick your file",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                            {DevicePlatform.Android, new[] { "application/octet-stream" } }
                    })
            });
            if (result != null)
            {
                return result.FullPath;
            }
            return null;
        }
    }
}