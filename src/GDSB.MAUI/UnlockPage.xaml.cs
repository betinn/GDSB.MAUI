using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class UnlockPage : ContentPage
{
    private readonly UnlockViewModel _viewModel;

    public UnlockPage(UnlockViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        BrandMark.Drawable = new BrandMarkDrawable();
        // No botão a digital é branca e sem a crista curta, como no protótipo: em 18px
        // ela vira um risco solto.
        BiometricButtonIcon.Drawable = new FingerprintDrawable
        {
            Stroke = Colors.White,
            StrokeWidth = 1.7f,
            Compact = true,
        };
    }

    // Mantém o conteúdo com pelo menos a altura visível: assim a linha "*" continua centralizando
    // o bloco de desbloqueio quando sobra espaço, e o ScrollView assume quando não cabe (janela
    // baixa no Windows, celular pequeno). Sem isto seria um ou outro - ou o conteúdo cola no topo
    // sempre, ou nunca rola.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (height > 0)
            UnlockContent.MinimumHeightRequest = Math.Max(0, height - RootScroll.Padding.VerticalThickness);
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
