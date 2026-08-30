using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class CreateVaultPage : ContentPage
{
    public CreateVaultPage(CreateVaultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
