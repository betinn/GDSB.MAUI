using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class UnlockPage : ContentPage
{
    private readonly UnlockViewModel _viewModel;

    public UnlockPage(UnlockViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ClearPassword();
        await _viewModel.RefreshBiometricAvailabilityAsync();
    }
}
