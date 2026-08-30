using CommunityToolkit.Mvvm.ComponentModel;
using GDSB.Domain.Entities;

namespace GDSB.MAUI.ViewModels
{
    // Envolve um SecretBox com propriedades já prontas pra UI (inicial do avatar, etc.),
    // em vez de espalhar conversores pelo XAML.
    public partial class SecretBoxItemViewModel : ObservableObject
    {
        public SecretBox Box { get; }

        public SecretBoxItemViewModel(SecretBox box)
        {
            Box = box;
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
