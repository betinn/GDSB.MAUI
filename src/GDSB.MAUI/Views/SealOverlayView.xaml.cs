namespace GDSB.MAUI.Views;

public partial class SealOverlayView : ContentView
{
    private const string AnimationName = "GdsbLockSeal";

    private readonly LockSealDrawable _drawable = new();
    private CancellationTokenSource? _playCts;

    public SealOverlayView()
    {
        InitializeComponent();
        SealView.Drawable = _drawable;
    }

    /// <summary>
    /// Toca o selo por cima de tudo que a página hospedeira já mostra. Chamado só quando a
    /// gravação que ele confirma deu certo.
    /// </summary>
    public async Task PlayAsync(LockSealMode mode, string label, uint length)
    {
        // Troca o token source antes de cancelar o anterior: com o await do CancelAsync no
        // meio, assumir o overlay primeiro evita que duas ações quase simultâneas se
        // intercalem entre o cancelamento e a atribuição.
        var previous = _playCts;
        var cts = new CancellationTokenSource();
        _playCts = cts;

        if (previous is not null)
            await previous.CancelAsync();

        this.AbortAnimation(AnimationName);

        _drawable.Mode = mode;
        _drawable.Progress = 0f;
        SealLabel.Text = label;
        SealView.Invalidate();
        Opacity = 0;
        IsVisible = true;

        try
        {
            await this.FadeToAsync(1, 90);
            await RunAnimationAsync(length);
            await Task.Delay(420, cts.Token);
            await this.FadeToAsync(0, 200);

            // Se outro selo assumiu o overlay durante o fade, quem esconde é ele.
            if (_playCts == cts)
                IsVisible = false;
        }
        catch (TaskCanceledException)
        {
            // Outro selo chegou antes do fim - a chamada nova assume o overlay.
        }
    }

    /// <summary>
    /// Linear de propósito: o escalonamento de cada fase (queda da haste, repique, anel)
    /// já está no LockSealDrawable, que precisa receber o tempo cru para compô-las.
    /// </summary>
    private Task RunAnimationAsync(uint length)
    {
        var tcs = new TaskCompletionSource();

        new Microsoft.Maui.Controls.Animation(
                v =>
                {
                    _drawable.Progress = (float)v;
                    SealView.Invalidate();
                }, 0d, 1d)
            .Commit(this, AnimationName, length: length, easing: Easing.Linear,
                finished: (_, _) => tcs.TrySetResult());

        return tcs.Task;
    }
}
