namespace GDSB.MAUI.Views;

/// <summary>
/// Embrulha o <see cref="TapHintDrawable"/> num controle que se anima sozinho enquanto está na
/// tela. Usado nas amostras do painel de ajuda: cada amostra é uma réplica inerte do controle
/// real, e por cima dela vai um destes apontando onde o usuário tocaria.
///
/// É decorativo por definição - InputTransparent, e sem SemanticProperties, porque quem tem o
/// texto acessível é a legenda da amostra, não o indicador.
/// </summary>
public sealed class TapHintView : ContentView
{
    private const string AnimationName = "GdsbTapHint";

    // Um ciclo completo (mão desce, volta e os anéis se abrem). Devagar de propósito: o indicador
    // fica em laço enquanto o painel estiver aberto, e um ritmo mais curto vira pisca-pisca.
    private const uint CycleLength = 1700;

    private readonly TapHintDrawable _drawable = new();
    private readonly GraphicsView _canvas;

    public TapHintView()
    {
        _canvas = new GraphicsView
        {
            Drawable = _drawable,
            WidthRequest = 34,
            HeightRequest = 34,
            BackgroundColor = Colors.Transparent,
        };

        Content = _canvas;
        InputTransparent = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e) => StartLoop();

    private void OnUnloaded(object? sender, EventArgs e) => this.AbortAnimation(AnimationName);

    private void StartLoop()
    {
        this.AbortAnimation(AnimationName);

        // Linear: a curva do toque (a mão descendo e voltando) já é o seno aplicado dentro do
        // drawable - somar um Easing aqui deformaria as duas coisas ao mesmo tempo.
        new Microsoft.Maui.Controls.Animation(
                v =>
                {
                    _drawable.Progress = (float)v;
                    _canvas.Invalidate();
                }, 0d, 1d)
            .Commit(this, AnimationName, length: CycleLength, easing: Easing.Linear, repeat: () => true);
    }
}
