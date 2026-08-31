using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class VaultPage : ContentPage
{
    private const string SealAnimationName = "GdsbLockSeal";

    private readonly VaultViewModel _viewModel;
    private readonly LockSealDrawable _sealDrawable = new();
    private CancellationTokenSource? _toastCts;
    private CancellationTokenSource? _sealCts;

    public VaultPage(VaultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.ToastRequested += OnToastRequested;
        _viewModel.SecretCreated += OnSecretCreated;
        SealView.Drawable = _sealDrawable;
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

    /// <summary>
    /// Cadeado da marca fechando, por cima da lista já atualizada. Só chega aqui quando o
    /// item é novo e a gravação no arquivo deu certo - ver VaultViewModel.SecretCreated.
    /// </summary>
    private async void OnSecretCreated(object? sender, EventArgs e)
    {
        // Troca o token source antes de cancelar o anterior: com o await do CancelAsync no
        // meio, assumir o overlay primeiro evita que duas criações quase simultâneas se
        // intercalem entre o cancelamento e a atribuição.
        var previous = _sealCts;
        var cts = new CancellationTokenSource();
        _sealCts = cts;

        if (previous is not null)
            await previous.CancelAsync();

        this.AbortAnimation(SealAnimationName);

        _sealDrawable.Progress = 0f;
        SealView.Invalidate();
        SealOverlay.Opacity = 0;
        SealOverlay.IsVisible = true;

        try
        {
            await SealOverlay.FadeToAsync(1, 90);
            await RunSealAnimationAsync();
            await Task.Delay(420, cts.Token);
            await SealOverlay.FadeToAsync(0, 200);

            // Se outra criação assumiu o overlay durante o fade, quem esconde é ela.
            if (_sealCts == cts)
                SealOverlay.IsVisible = false;
        }
        catch (TaskCanceledException)
        {
            // Outra criação chegou antes do fim - a chamada nova assume o overlay.
        }
    }

    /// <summary>
    /// Linear de propósito: o escalonamento de cada fase (queda da haste, repique, anel)
    /// já está no LockSealDrawable, que precisa receber o tempo cru para compô-las.
    /// </summary>
    private Task RunSealAnimationAsync()
    {
        var tcs = new TaskCompletionSource();

        new Microsoft.Maui.Controls.Animation(
                v =>
                {
                    _sealDrawable.Progress = (float)v;
                    SealView.Invalidate();
                }, 0d, 1d)
            .Commit(this, SealAnimationName, length: 760, easing: Easing.Linear,
                finished: (_, _) => tcs.TrySetResult());

        return tcs.Task;
    }
}
