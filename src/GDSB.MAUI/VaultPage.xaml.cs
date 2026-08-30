using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class VaultPage : ContentPage
{
    private readonly VaultViewModel _viewModel;

    public VaultPage(VaultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _viewModel.OnSizeChanged(width);
    }
}
