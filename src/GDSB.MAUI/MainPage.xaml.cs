using GDSB.Domain.Entities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GDSB.MAUI
{
    public partial class MainPage : ContentPage
    {
        private readonly int maxItemsPage = 16;
        public ICommand OpenUrlCommand { get; }
        public ICommand CopyToClipboardCommand { get; }
        private Profile Profile { get; set; }
        private ObservableCollection<SecretBox> _filteredItems;
        public ObservableCollection<SecretBox> FilteredItems
        {
            get => _filteredItems;
            set
            {
                _filteredItems = value;
                OnPropertyChanged(nameof(FilteredItems));
            }
        }
        public MainPage(Profile profile)
        {
            InitializeComponent();

            OpenUrlCommand = new Command<string>(OpenUrl);
            CopyToClipboardCommand = new Command<string>(CopyToClipboard);

            Profile = profile;
            FilteredItems = new ObservableCollection<SecretBox>(Profile.Boxes.Take(maxItemsPage).OrderBy(x => x.Favorito));

            BindingContext = this;
        }
        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = e.NewTextValue;
            if (string.IsNullOrWhiteSpace(filter))
            {
                FilteredItems = new ObservableCollection<SecretBox>(Profile.Boxes.Take(maxItemsPage).ToList().OrderBy(x => x.Favorito));
            }
            else
            {
                FilteredItems = new ObservableCollection<SecretBox>(
                    Profile.Boxes.Where(item => item.BoxName.ToLower().Contains(filter.ToLower())).Take(maxItemsPage).ToList().OrderBy(x => x.Favorito));
            }
        }

        public void OpenUrl(string url)
        {
            try
            {
                Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                DisplayAlert(null, $"Erro ao tentar abrir {url} no navegador padrao {ex.Message}", "Ok");
            }
        }

        public void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetTextAsync(text);
            }
            catch (Exception ex)
            {
                DisplayAlert(null, $"Erro ao tentar copiar senha para area de transferencia {ex.Message}", "Ok");
            }
        }
    }

}
