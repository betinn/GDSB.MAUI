using GDSB.MAUI.Interfaces;

namespace GDSB.MAUI;

public partial class FileFinderDecrypter : ContentPage
{
	private readonly IFilePickerService _filePickerService;
	private readonly IFileDataService _fileDataService;
	public FileFinderDecrypter(IFilePickerService filePickerService, IFileDataService fileDataService)
	{
		InitializeComponent();
		_filePickerService = filePickerService;
		_fileDataService = fileDataService;
	}

    private async void FilePickBtn_Clicked(object sender, EventArgs e)
    {
		if (string.IsNullOrEmpty(PasswordEntry.Text))
		{
            await DisplayAlert(null, "Please enter your password first.", "Ok! I understood!");
			return;
        }

		var fileName = await _filePickerService.PickFileNameAsync();

		var profile = _fileDataService.GetProfile(fileName, PasswordEntry.Text);
    }
}