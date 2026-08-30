using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;

// VaultPage vive no namespace raiz GDSB.MAUI (namespace mãe deste), resolvida sem using extra.
namespace GDSB.MAUI.ViewModels
{
    public partial class CreateVaultViewModel : ObservableObject
    {
        private const int MinPasswordLength = 8;

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;

        public CreateVaultViewModel(
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService)
        {
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
        }

        [ObservableProperty]
        private string vaultName = "Meu Cofre";

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;

        public string CreateButtonText => IsBusy ? "Criando..." : "Criar cofre";

        [RelayCommand]
        private Task GoBackAsync() => _navigationService.GoBackAsync();

        [RelayCommand(CanExecute = nameof(CanCreate))]
        private async Task CreateVaultAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(VaultName))
            {
                ErrorMessage = "Dê um nome ao cofre.";
                return;
            }

            if (Password.Length < MinPasswordLength)
            {
                ErrorMessage = $"A senha mestra precisa ter pelo menos {MinPasswordLength} caracteres.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "As senhas não coincidem.";
                return;
            }

            string? location;
            try
            {
                location = await _filePickerService.PickSaveLocationAsync($"{VaultName}.GDSBX");
            }
            catch (Exception)
            {
                ErrorMessage = "Não foi possível escolher onde salvar o cofre.";
                return;
            }

            if (string.IsNullOrEmpty(location))
                return;

            IsBusy = true;
            try
            {
                var profile = new Profile { Nome = VaultName.Trim() };
                var enteredPassword = Password;

                await Task.Run(() => _profileFileService.Save(location, profile, enteredPassword));

                await _navigationService.NavigateToRootAsync(nameof(VaultPage), new Dictionary<string, object>
                {
                    ["Profile"] = profile,
                    ["Location"] = location,
                    ["Password"] = enteredPassword,
                });
            }
            catch (Exception)
            {
                ErrorMessage = "Não foi possível criar o cofre nesse local.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCreate() => !IsBusy;

        partial void OnIsBusyChanged(bool value)
        {
            CreateVaultCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
            OnPropertyChanged(nameof(CreateButtonText));
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));
    }
}
