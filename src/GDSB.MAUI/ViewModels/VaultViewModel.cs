using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;
using GDSB.MAUI.Constants;
using GDSB.MAUI.Services;
using System.Collections.ObjectModel;

namespace GDSB.MAUI.ViewModels
{
    public partial class VaultViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IClipboardService _clipboardService;
        private readonly IAlertService _alertService;

        private Profile? _profile;

        public VaultViewModel(IClipboardService clipboardService, IAlertService alertService)
        {
            _clipboardService = clipboardService;
            _alertService = alertService;
        }

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
        private bool isPasswordVisible;

        [ObservableProperty]
        private bool isWideLayout;

        [ObservableProperty]
        private bool isConfirmingDelete;

        public string PasswordDisplay => SelectedItem is null
            ? string.Empty
            : (IsPasswordVisible ? SelectedItem.Pass : "••••••••••");

        public string RevealPasswordGlyph => IsPasswordVisible ? "🙈" : "👁";

        public bool ShowItemActions => !IsConfirmingDelete;

        public string ConfirmDeleteMessage => SelectedItem is null
            ? string.Empty
            : $"Excluir \"{SelectedItem.BoxName}\" do cofre? Essa ação não pode ser desfeita.";

        public bool IsCompactLayout => !IsWideLayout;

        public bool IsAllFilterSelected => !FilterFavoritesOnly;

        public bool HasSelectedItem => SelectedItem is not null;

        public bool HasNoSelection => SelectedItem is null;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Profile", out var value) && value is Profile profile)
            {
                _profile = profile;
                VaultName = profile.Nome;
                RefreshItems();
            }
        }

        public void OnSizeChanged(double width) => IsWideLayout = width >= ResponsiveBreakpoints.TabletMinWidth;

        partial void OnSearchTextChanged(string value) => RefreshItems();

        partial void OnFilterFavoritesOnlyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAllFilterSelected));
            RefreshItems();
        }

        partial void OnIsWideLayoutChanged(bool value) => OnPropertyChanged(nameof(IsCompactLayout));

        partial void OnSelectedItemChanged(SecretBoxItemViewModel? value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
            OnPropertyChanged(nameof(ConfirmDeleteMessage));
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(HasNoSelection));
        }

        partial void OnIsPasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
            OnPropertyChanged(nameof(RevealPasswordGlyph));
        }

        partial void OnIsConfirmingDeleteChanged(bool value) => OnPropertyChanged(nameof(ShowItemActions));

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

            SelectedItem = selectedBox is null ? null : Items.FirstOrDefault(i => i.Box == selectedBox);
        }

        [RelayCommand]
        private void SetFilterAll() => FilterFavoritesOnly = false;

        [RelayCommand]
        private void SetFilterFavorites() => FilterFavoritesOnly = true;

        [RelayCommand]
        private void ToggleFavorite(SecretBoxItemViewModel item)
        {
            item.Box.Favorito = !item.Box.Favorito;
            RefreshItems();
        }

        [RelayCommand]
        private void OpenEditor(SecretBoxItemViewModel item)
        {
            SelectedItem = item;
            IsPasswordVisible = false;
            IsConfirmingDelete = false;
            IsEditorOpen = true;
        }

        [RelayCommand]
        private void CloseEditor()
        {
            IsEditorOpen = false;
            IsPasswordVisible = false;
            IsConfirmingDelete = false;
        }

        [RelayCommand]
        private void ToggleRevealPassword() => IsPasswordVisible = !IsPasswordVisible;

        [RelayCommand]
        private void PromptDelete() => IsConfirmingDelete = true;

        [RelayCommand]
        private void CancelDelete() => IsConfirmingDelete = false;

        [RelayCommand]
        private void ConfirmDelete(SecretBoxItemViewModel item)
        {
            _profile?.Boxes.Remove(item.Box);

            if (SelectedItem == item)
                SelectedItem = null;

            IsConfirmingDelete = false;
            IsEditorOpen = false;
            RefreshItems();
        }

        [RelayCommand]
        private async Task CopyUserAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.User))
                return;

            await _clipboardService.SetTextAsync(item.User);
        }

        [RelayCommand]
        private async Task CopyPasswordAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.Pass))
                return;

            await _clipboardService.SetTextAsync(item.Pass);
        }

        [RelayCommand]
        private async Task OpenUrlAsync(SecretBoxItemViewModel item)
        {
            if (string.IsNullOrEmpty(item.Url))
                return;

            try
            {
                var url = item.Url.Contains("://") ? item.Url : $"https://{item.Url}";
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                await _alertService.DisplayAlertAsync(null, $"Erro ao tentar abrir {item.Url}: {ex.Message}", "Ok");
            }
        }
    }
}
