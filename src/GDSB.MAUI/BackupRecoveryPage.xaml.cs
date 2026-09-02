using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class BackupRecoveryPage : ContentPage
{
    private readonly BackupRecoveryViewModel _viewModel;

    public BackupRecoveryPage(BackupRecoveryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}
