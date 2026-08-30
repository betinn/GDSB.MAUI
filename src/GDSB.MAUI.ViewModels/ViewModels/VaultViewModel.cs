using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Constants;
using GDSB.MAUI.Services;
using System.Collections.ObjectModel;

namespace GDSB.MAUI.ViewModels
{
    public partial class VaultViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IClipboardService _clipboardService;
        private readonly IAlertService _alertService;
        private readonly IProfileFileService _profileFileService;
        private readonly INavigationService _navigationService;
        private readonly IAppLauncherService _appLauncherService;

        private Profile? _profile;
        private string? _location;
        private string? _password;

        public VaultViewModel(
            IClipboardService clipboardService,
            IAlertService alertService,
            IProfileFileService profileFileService,
            INavigationService navigationService,
            IAppLauncherService appLauncherService)
        {
            _clipboardService = clipboardService;
            _alertService = alertService;
            _profileFileService = profileFileService;
            _navigationService = navigationService;
            _appLauncherService = appLauncherService;
        }

        // A View anima um toast quando isso dispara - decidido por evento (não por uma propriedade
        // IsVisible) porque a duração/fade é responsabilidade de apresentação, não de estado.
        public event EventHandler<string>? ToastRequested;

        public ObservableCollection<SecretBoxItemViewModel> Items { get; } = new();

        [ObservableProperty]
        private string vaultName = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool filterFavoritesOnly;

        [ObservableProperty]
        private SecretBoxItemViewModel? selectedItem;

        [ObservableProperty]
        private bool isEditorOpen;

        [ObservableProperty]
        private bool isEditingItem;

        [ObservableProperty]
        private bool isPasswordVisible;

        [ObservableProperty]
        private bool isWideLayout;

        [ObservableProperty]
        private bool isConfirmingDelete;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string editBoxName = string.Empty;

        [ObservableProperty]
        private string editUrl = string.Empty;

        [ObservableProperty]
        private string editUser = string.Empty;

        [ObservableProperty]
        private string editPassword = string.Empty;

        [ObservableProperty]
        private string editObs = string.Empty;

        [ObservableProperty]
        private bool editFavorito;

        [ObservableProperty]
        private string? validationError;

        public string PasswordDisplay => SelectedItem is null
            ? string.Empty
            : (IsPasswordVisible ? SelectedItem.Pass : "••••••••••");

        public string RevealPasswordGlyph => IsPasswordVisible ? "🙈" : "👁";

        public bool ShowItemActions => !IsConfirmingDelete && IsViewingItem;

        public string ConfirmDeleteMessage => SelectedItem is null
            ? string.Empty
            : $"Excluir \"{SelectedItem.BoxName}\" do cofre? Essa ação não pode ser desfeita.";

        public bool IsCompactLayout => !IsWideLayout;

        // O bottom-sheet do editor só existe no layout compacto (celular): no layout largo (tablet)
        // o mesmo editor já aparece no painel lateral, indexado por ShowItemEditor.
        public bool IsCompactEditorOpen => IsEditorOpen && IsCompactLayout;

        public bool IsAllFilterSelected => !FilterFavoritesOnly;

        public bool HasSelectedItem => SelectedItem is not null;

        public bool HasNoSelection => SelectedItem is null;

        public bool IsViewingItem => !IsEditingItem;

        public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

        public bool CanInteract => !IsBusy;

        // No layout largo o painel lateral mostra o editor tanto pra um item selecionado quanto
        // pra um item novo sendo criado (que ainda não tem SelectedItem) - por isso não dá pra usar
        // só HasSelectedItem aqui, senão "Novo item" cairia direto na mensagem de "nada selecionado".
        public bool ShowItemEditor => HasSelectedItem || IsEditingItem;

        public bool ShowEmptyState => !ShowItemEditor;

        public string EditorHeaderTitle => SelectedItem is null ? "Novo item" : SelectedItem.BoxName;

        public string EditorHeaderInitial => SelectedItem?.Initial ?? "+";

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Profile", out var profileValue) && profileValue is Profile profile)
            {
                _profile = profile;
                VaultName = profile.Nome;
                RefreshItems();
            }

            if (query.TryGetValue("Location", out var locationValue) && locationValue is string location)
                _location = location;

            if (query.TryGetValue("Password", out var passwordValue) && passwordValue is string password)
                _password = password;
        }

        public void OnSizeChanged(double width) => IsWideLayout = width >= ResponsiveBreakpoints.TabletMinWidth;

        partial void OnSearchTextChanged(string value) => RefreshItems();

        partial void OnFilterFavoritesOnlyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAllFilterSelected));
            RefreshItems();
        }

        partial void OnIsWideLayoutChanged(bool value)
        {
            OnPropertyChanged(nameof(IsCompactLayout));
            OnPropertyChanged(nameof(IsCompactEditorOpen));
        }

        partial void OnIsEditorOpenChanged(bool value) => OnPropertyChanged(nameof(IsCompactEditorOpen));

        partial void OnIsEditingItemChanged(bool value)
        {
            OnPropertyChanged(nameof(IsViewingItem));
            OnPropertyChanged(nameof(ShowItemActions));
            OnPropertyChanged(nameof(ShowItemEditor));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        partial void OnSelectedItemChanged(SecretBoxItemViewModel? value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
            OnPropertyChanged(nameof(ConfirmDeleteMessage));
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(HasNoSelection));
            OnPropertyChanged(nameof(ShowItemEditor));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EditorHeaderTitle));
            OnPropertyChanged(nameof(EditorHeaderInitial));
        }

        partial void OnIsPasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
            OnPropertyChanged(nameof(RevealPasswordGlyph));
        }

        partial void OnIsConfirmingDeleteChanged(bool value) => OnPropertyChanged(nameof(ShowItemActions));

        partial void OnValidationErrorChanged(string? value) => OnPropertyChanged(nameof(HasValidationError));

        partial void OnIsBusyChanged(bool value)
        {
            SaveItemCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanInteract));
        }

        private void RefreshItems()
        {
            var selectedBox = SelectedItem?.Box;

            Items.Clear();
            if (_profile is null)
                return;

            IEnumerable<SecretBox> boxes = _profile.Boxes;

            if (FilterFavoritesOnly)
                boxes = boxes.Where(b => b.Favorito);

            if (!string.IsNullOrWhiteSpace(SearchText))
                boxes = boxes.Where(b => b.BoxName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var box in boxes.OrderByDescending(b => b.Favorito).ThenBy(b => b.BoxName))
                Items.Add(new SecretBoxItemViewModel(box));

            // Reaponta a seleção para o VM novo que embrulha o mesmo SecretBox. Se o item saiu da
            // lista (filtro/busca), não há o que mostrar no editor - fecha em vez de deixar vazio.
            // Isso não deve interromper um "Novo item" em andamento (SelectedItem já é null de
            // propósito nesse caso, e não é ele que saiu de uma lista).
            SelectedItem = selectedBox is null ? null : Items.FirstOrDefault(i => i.Box == selectedBox);

            if (SelectedItem is null && IsEditorOpen && !IsEditingItem)
                CloseEditor();
        }

        [RelayCommand]
        private Task GoHomeAsync() => _navigationService.GoHomeAsync();

        private async Task PersistAsync()
        {
            if (_profile is null || _location is null || _password is null)
                return;

            IsBusy = true;
            try
            {
                var location = _location;
                var password = _password;
                var profile = _profile;
                await Task.Run(() => _profileFileService.Save(location, profile, password));
            }
            catch (Exception)
            {
                await _alertService.DisplayAlertAsync(null, "Não foi possível salvar o cofre. Tente novamente.", "Ok");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void SetFilterAll() => FilterFavoritesOnly = false;

        [RelayCommand]
        private void SetFilterFavorites() => FilterFavoritesOnly = true;

        [RelayCommand]
        private async Task ToggleFavoriteAsync(SecretBoxItemViewModel item)
        {
            item.Box.Favorito = !item.Box.Favorito;
            await PersistAsync();
            RefreshItems();
        }

        [RelayCommand]
        private void OpenEditor(SecretBoxItemViewModel item)
        {
            SelectedItem = item;
            IsEditingItem = false;
            IsPasswordVisible = false;
            IsConfirmingDelete = false;
            IsEditorOpen = true;
        }

        [RelayCommand]
        private void AddNewItem()
        {
            SelectedItem = null;
            EditBoxName = string.Empty;
            EditUrl = string.Empty;
            EditUser = string.Empty;
            EditPassword = string.Empty;
            EditObs = string.Empty;
            EditFavorito = false;
            ValidationError = null;
            IsConfirmingDelete = false;
            IsEditingItem = true;
            IsEditorOpen = true;
        }

        [RelayCommand]
        private void EditItem(SecretBoxItemViewModel item)
        {
            SelectedItem = item;
            EditBoxName = item.Box.BoxName;
            EditUrl = item.Box.Url;
            EditUser = item.Box.User;
            EditPassword = item.Box.Pass;
            EditObs = item.Box.Obs;
            EditFavorito = item.Box.Favorito;
            ValidationError = null;
            IsPasswordVisible = false;
            IsConfirmingDelete = false;
            IsEditingItem = true;
            IsEditorOpen = true;
        }

        [RelayCommand]
        private void CancelEditItem()
        {
            IsEditingItem = false;
            ValidationError = null;

            if (SelectedItem is null)
                IsEditorOpen = false;
        }

        [RelayCommand(CanExecute = nameof(CanSaveItem))]
        private async Task SaveItemAsync()
        {
            if (string.IsNullOrWhiteSpace(EditBoxName))
            {
                ValidationError = "Informe um nome para o item.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditPassword))
            {
                ValidationError = "Informe a senha do item.";
                return;
            }

            ValidationError = null;

            if (SelectedItem is null)
            {
                var box = new SecretBox
                {
                    BoxName = EditBoxName.Trim(),
                    Url = EditUrl.Trim(),
                    User = EditUser.Trim(),
                    Pass = EditPassword,
                    Obs = EditObs.Trim(),
                    Favorito = EditFavorito,
                };
                _profile?.Boxes.Add(box);
            }
            else
            {
                var box = SelectedItem.Box;
                box.BoxName = EditBoxName.Trim();
                box.Url = EditUrl.Trim();
                box.User = EditUser.Trim();
                box.Pass = EditPassword;
                box.Obs = EditObs.Trim();
                box.Favorito = EditFavorito;
            }

            await PersistAsync();

            IsEditingItem = false;
            IsEditorOpen = false;
            RefreshItems();
        }

        private bool CanSaveItem() => !IsBusy;

        [RelayCommand]
        private void CloseEditor()
        {
            IsEditorOpen = false;
            IsEditingItem = false;
            IsPasswordVisible = false;
            IsConfirmingDelete = false;

            // No layout largo (tablet) o painel lateral fica visível enquanto ShowItemEditor for
            // true (HasSelectedItem || IsEditingItem) - sem limpar a seleção aqui, o "X" fechava
            // IsEditorOpen (que só controla o bottom-sheet do celular) e o painel do tablet, que
            // não depende disso, continuava aberto do mesmo jeito.
            SelectedItem = null;
        }

        [RelayCommand]
        private void ToggleRevealPassword() => IsPasswordVisible = !IsPasswordVisible;

        [RelayCommand]
        private void PromptDelete() => IsConfirmingDelete = true;

        [RelayCommand]
        private void CancelDelete() => IsConfirmingDelete = false;

        [RelayCommand]
        private async Task ConfirmDeleteAsync(SecretBoxItemViewModel item)
        {
            _profile?.Boxes.Remove(item.Box);

            if (SelectedItem == item)
                SelectedItem = null;

            IsConfirmingDelete = false;
            IsEditorOpen = false;

            await PersistAsync();
            RefreshItems();
        }

        [RelayCommand]
        private async Task CopyUserAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.User))
                return;

            await _clipboardService.SetTextAsync(item.User);
            ToastRequested?.Invoke(this, "Usuário copiado");
        }

        [RelayCommand]
        private async Task CopyPasswordAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.Pass))
                return;

            await _clipboardService.SetTextAsync(item.Pass);
            ToastRequested?.Invoke(this, "Senha copiada");
        }

        [RelayCommand]
        private async Task OpenUrlAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.Url))
                return;

            try
            {
                var url = item.Url.Contains("://") ? item.Url : $"https://{item.Url}";
                await _appLauncherService.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                await _alertService.DisplayAlertAsync(null, $"Erro ao tentar abrir {item.Url}: {ex.Message}", "Ok");
            }
        }
    }
}
