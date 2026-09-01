using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class VaultSettingsPage : ContentPage
{
    public VaultSettingsPage(VaultSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
