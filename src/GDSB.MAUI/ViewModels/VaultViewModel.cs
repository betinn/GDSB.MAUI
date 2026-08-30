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

        // O bottom-sheet do editor só existe no layout compacto (celular): no layout largo (tablet)
        // o mesmo editor já aparece no painel lateral, indexado por HasSelectedItem.
        public bool IsCompactEditorOpen => IsEditorOpen && IsCompactLayout;

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

        partial void OnIsWideLayoutChanged(bool value)
        {
            OnPropertyChanged(nameof(IsCompactLayout));
            OnPropertyChanged(nameof(IsCompactEditorOpen));
        }

        partial void OnIsEditorOpenChanged(bool value) => OnPropertyChanged(nameof(IsCompactEditorOpen));

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

            // Reaponta a seleção para o VM novo que embrulha o mesmo SecretBox. Se o item saiu da
            // lista (filtro/busca), não há o que mostrar no editor — fecha em vez de deixar vazio.
            SelectedItem = selectedBox is null ? null : Items.FirstOrDefault(i => i.Box == selectedBox);

            if (SelectedItem is null && IsEditorOpen)
                CloseEditor();
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
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                await _alertService.DisplayAlertAsync(null, $"Erro ao tentar abrir {item.Url}: {ex.Message}", "Ok");
            }
        }
    }
}
