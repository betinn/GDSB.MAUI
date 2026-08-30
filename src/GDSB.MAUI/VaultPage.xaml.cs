using GDSB.MAUI.ViewModels;

namespace GDSB.MAUI;

public partial class VaultPage : ContentPage
{
    private readonly VaultViewModel _viewModel;
    private CancellationTokenSource? _toastCts;

    public VaultPage(VaultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.ToastRequested += OnToastRequested;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _viewModel.OnSizeChanged(width);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        // O teclado virtual pode continuar aberto do Entry de senha da tela anterior
        // (Unlock/CreateVault) - fecha explicitamente ao entrar no cofre.
        Platforms.Android.KeyboardDismissal.Hide();
#endif
    }

    private async void OnToastRequested(object? sender, string message)
    {
        _toastCts?.Cancel();
        var cts = new CancellationTokenSource();
        _toastCts = cts;

        ToastLabel.Text = message;
        await ToastBorder.FadeTo(1, 120);

        try
        {
            await Task.Delay(1600, cts.Token);
            await ToastBorder.FadeTo(0, 220);
        }
        catch (TaskCanceledException)
        {
            // Um novo toast chegou antes do delay acabar - a chamada nova assume o fade-out.
        }
    }
}
