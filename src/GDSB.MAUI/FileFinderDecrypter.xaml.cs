using GDSB.MAUI.Interfaces;

namespace GDSB.MAUI;

public partial class FileFinderDecrypter : ContentPage
{
	private readonly IFilePickerService _filePickerService;
	private readonly IFileDataService _fileDataService;
	public FileFinderDecrypter()
	{
		InitializeComponent();
		_filePickerService = App.ServiceProvider.GetRequiredService<IFilePickerService>();
        _fileDataService = App.ServiceProvider.GetRequiredService<IFileDataService>();
    }

    private async void FilePickBtn_Clicked(object sender, EventArgs e)
    {
		if (string.IsNullOrEmpty(PasswordEntry.Text))
		{
            await DisplayAlert(null, "Please enter your password first.", "Ok! I understood!");
			return;
        }
		try
		{
			var fileName = await _filePickerService.PickFileNameAsync();

			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("Arquivo está vazio ou null", fileName);

            var profile = _fileDataService.GetProfile(fileName, PasswordEntry.Text);

            await Navigation.PushAsync(new MainPage(profile), true);
        }
		catch (Exception ex)
		{
			await DisplayAlert("Erro grave!", $"Erro: {ex.Message}", "blz");
		}

        
    }
}