using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

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
        _viewModel.SecretCreated += OnSecretCreated;
        _viewModel.SecretUpdated += OnSecretUpdated;
        _viewModel.SecretDeleted += OnSecretDeleted;
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

    // Os três selos, por cima da lista já atualizada. Só chegam aqui quando a gravação no
    // arquivo deu certo - ver os eventos correspondentes no VaultViewModel.
    private async void OnSecretCreated(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Create, "Segredo guardado", 760);

    private async void OnSecretUpdated(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Update, "Alterações salvas", 620);

    private async void OnSecretDeleted(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Delete, "Segredo excluído", 740);
}
