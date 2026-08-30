using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using GDSB.MAUI.Services;

// VaultPage vive no namespace raiz GDSB.MAUI (namespace mãe deste), resolvida sem using extra.
namespace GDSB.MAUI.ViewModels
{
    public partial class UnlockViewModel : ObservableObject
    {
        // Senha errada e arquivo corrompido devem ser indistinguíveis pra quem usa o app -
        // nunca mostrar ex.Message cru, sempre essa mensagem genérica.
        private const string GenericErrorMessage = "Senha incorreta ou arquivo corrompido.";
        private const string EmptyPasswordMessage = "Digite a senha mestra do cofre.";
        private const string FilePickerErrorMessage = "Não foi possível abrir o seletor de arquivos.";

        private readonly IProfileFileService _profileFileService;
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;

        public UnlockViewModel(
            IProfileFileService profileFileService,
            IFilePickerService filePickerService,
            INavigationService navigationService)
        {
            _profileFileService = profileFileService;
            _filePickerService = filePickerService;
            _navigationService = navigationService;
        }

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isPasswordHidden = true;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public bool CanInteract => !IsBusy;

        public string EyeGlyph => IsPasswordHidden ? "👁" : "🙈";

        [RelayCommand]
        private void ToggleShowPassword() => IsPasswordHidden = !IsPasswordHidden;

        public void ClearPassword()
        {
            Password = string.Empty;
            ErrorMessage = null;
            IsPasswordHidden = true;
        }

        [RelayCommand(CanExecute = nameof(CanUnlock))]
        private async Task UnlockAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = EmptyPasswordMessage;
                return;
            }

            string? filePath;
            try
            {
                filePath = await _filePickerService.PickFileNameAsync();
            }
            catch (Exception)
            {
                ErrorMessage = FilePickerErrorMessage;
                return;
            }

            if (string.IsNullOrEmpty(filePath))
                return;

            IsBusy = true;
            try
            {
                var enteredPassword = Password;
                var result = await Task.Run(() => _profileFileService.Open(filePath, enteredPassword));

                if (result.WasLegacyFormat)
                    await Task.Run(() => _profileFileService.Save(filePath, result.Profile, enteredPassword));

                ClearPassword();

                await _navigationService.NavigateToAsync(nameof(VaultPage), new Dictionary<string, object>
                {
                    ["Profile"] = result.Profile,
                });
            }
            catch (Exception)
            {
                // Cobre tanto InvalidPasswordOrCorruptFileException (v2) quanto as exceções do
                // leitor legado (v1) - a mensagem pro usuário é sempre a mesma, de propósito.
                ErrorMessage = GenericErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanUnlock() => !IsBusy;

        partial void OnIsBusyChanged(bool value)
        {
            UnlockCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
        }

        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasErrorMessage));

        partial void OnIsPasswordHiddenChanged(bool value) => OnPropertyChanged(nameof(EyeGlyph));
    }
}
