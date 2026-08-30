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

        // Window.Resumed cobre o app voltando de background enquanto a UnlockPage já estava
        // "appeared" (não navegou pra fora e voltou) - sem isso, sair do app e voltar (trocar de
        // app, tela de apps recentes etc.) sem exceder o timeout do auto-lock não re-oferecia a
        // biometria sozinha, só funcionava quando a página fazia OnAppearing de verdade de novo.
        if (Window is not null)
            Window.Resumed += OnWindowResumed;

        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (Window is not null)
            Window.Resumed -= OnWindowResumed;
    }

    private async void OnWindowResumed(object? sender, EventArgs e) => await _viewModel.InitializeAsync();
}
