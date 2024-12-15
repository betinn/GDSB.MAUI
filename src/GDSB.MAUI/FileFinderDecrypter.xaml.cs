using GDSB.MAUI.Interfaces;

namespace GDSB.MAUI;

public partial class FileFinderDecrypter : ContentPage
{
	private readonly IFilePickerService _filePickerService;
	public FileFinderDecrypter(IFilePickerService filePickerService)
	{
		InitializeComponent();
		_filePickerService = filePickerService;
	}

    private async void FilePickBtn_Clicked(object sender, EventArgs e)
    {
		if (string.IsNullOrEmpty(PasswordEntry.Text))
		{
            await DisplayAlert("Alert", "Please enter your password first.", "OK");
			return;
        }

		var fileName = await _filePickerService.PickFileNameAsync();
    }
}