using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class VaultSettingsPage : ContentPage
{
    private readonly VaultSettingsViewModel _viewModel;

    public VaultSettingsPage(VaultSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.SettingsSaved += OnSettingsSaved;
    }

    private async void OnSettingsSaved(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Update, "Alterações salvas", 620);
}
