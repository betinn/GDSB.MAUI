using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class CreateVaultPage : ContentPage
{
    public CreateVaultPage(CreateVaultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Haste erguida: o cofre desta tela ainda não foi selado.
        BrandMark.Drawable = new BrandMarkDrawable { Open = true };
    }
}
