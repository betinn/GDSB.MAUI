using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GDSB.Domain.Entities;

namespace GDSB.MAUI.ViewModels
{
    // Envolve um SecretBox com propriedades já prontas pra UI (inicial do avatar, etc.),
    // em vez de espalhar conversores pelo XAML.
    public partial class SecretBoxItemViewModel : ObservableObject
    {
        private readonly VaultViewModel _owner;

        public SecretBox Box { get; }

        // Comandos próprios sem parâmetro, encaminhando pro comando do dono com "this" -
        // evita CommandParameter/RelativeSource no XAML.
        public IAsyncRelayCommand OpenUrlCommand { get; }
        public IAsyncRelayCommand ToggleFavoriteCommand { get; }
        public IAsyncRelayCommand CopyUserCommand { get; }
        public IAsyncRelayCommand CopyPasswordCommand { get; }
        public IRelayCommand OpenEditorCommand { get; }

        public SecretBoxItemViewModel(SecretBox box, VaultViewModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Box = box;
            _owner = owner;

            OpenUrlCommand = new AsyncRelayCommand(() => _owner.OpenUrlCommand.ExecuteAsync(this));
            ToggleFavoriteCommand = new AsyncRelayCommand(() => _owner.ToggleFavoriteCommand.ExecuteAsync(this));
            CopyUserCommand = new AsyncRelayCommand(() => _owner.CopyUserCommand.ExecuteAsync(this));
            CopyPasswordCommand = new AsyncRelayCommand(() => _owner.CopyPasswordCommand.ExecuteAsync(this));
            OpenEditorCommand = new RelayCommand(() => _owner.OpenEditorCommand.Execute(this));
        }

        public string BoxName => Box.BoxName;
        public string Url => Box.Url;
        public string User => Box.User;
        public string Pass => Box.Pass;
        public string Obs => Box.Obs;
        public bool HasObs => !string.IsNullOrWhiteSpace(Box.Obs);
        public bool HasUrl => !string.IsNullOrWhiteSpace(Box.Url);
        public bool HasUser => !string.IsNullOrWhiteSpace(Box.User);
        public bool Favorito => Box.Favorito;

        public string Initial => string.IsNullOrEmpty(Box.BoxName)
            ? "?"
            : Box.BoxName[..1].ToUpperInvariant();
    }
}
