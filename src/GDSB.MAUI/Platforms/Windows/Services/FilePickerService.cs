using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;
using Windows.Storage.Pickers;
using Windows.Storage;


namespace GDSB.MAUI.Platforms.Windows.Services
{
    public class FilePickerService : IFilePickerService
    {
        private readonly ILocalizationService _localization;

        public FilePickerService(ILocalizationService localization)
        {
            _localization = localization;
        }

        public async Task<PickedFile?> PickFileNameAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".GDSBX");

            WinRT.Interop.InitializeWithWindow.Initialize(picker, GetMainWindowHandle());

            StorageFile file = await picker.PickSingleFileAsync();
            return file is null ? null : new PickedFile(file.Path, file.Name);
        }

        public async Task<string> PickSaveLocationAsync(string suggestedName)
        {
            var picker = new FileSavePicker();
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add(_localization.Get("Platform_WindowsFileTypeLabel"), new List<string> { ".GDSBX" });

            WinRT.Interop.InitializeWithWindow.Initialize(picker, GetMainWindowHandle());

            // StorageFile.Path aqui já é um caminho de arquivo real (inclusive dentro de uma pasta
            // sincronizada, tipo OneDrive) - ao contrário do Android, não precisa de nenhum
            // tratamento especial de URI.
            StorageFile file = await picker.PickSaveFileAsync();
            return file?.Path ?? string.Empty;
        }

        // O diálogo do WinUI precisa ser ancorado numa janela nativa: no modelo do MAUI, App.Current,
        // o Handler da janela e o PlatformView são todos opcionais, mas um seletor só é aberto a
        // partir de uma janela na tela. Se nada disso estiver de pé não há hwnd para ancorar, e
        // seguir com um handle inválido faria o diálogo abrir sem dono (ou nem abrir) - os dois
        // chamadores nos ViewModels já tratam a exceção como "não deu para escolher o caminho".
        private static IntPtr GetMainWindowHandle()
        {
            var window = App.Current?.Windows.FirstOrDefault()
                ?? throw new InvalidOperationException("Nenhuma janela ativa para ancorar o seletor de arquivos.");

            if (window.Handler?.PlatformView is not MauiWinUIWindow platformWindow)
                throw new InvalidOperationException("A janela ativa ainda não tem uma janela nativa associada.");

            return platformWindow.WindowHandle;
        }
    }
}
