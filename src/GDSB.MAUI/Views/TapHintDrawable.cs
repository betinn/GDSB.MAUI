using Microsoft.Maui.Graphics;

namespace GDSB.MAUI.Views;

/// <summary>
/// A "mãozinha" que aparece por cima das amostras do painel de ajuda, apontando o controle que o
/// texto está explicando. É o que transforma o painel de "leia esta descrição" em "é este aqui,
/// e o toque é assim": a mão desce e sobe como num toque real, e o anel se abre no ponto de
/// contato, do mesmo jeito que o Android desenha o retorno de um toque.
///
/// Desenhada em vetor, e não como imagem ou emoji, pelo mesmo motivo do FingerprintDrawable e do
/// BrandMarkDrawable: emoji muda de desenho a cada plataforma e não aceita a cor da marca.
///
/// O desenho vive num espaço 24x24 escalado para o tamanho da view, com a ponta do dedo em
/// (12, 3) - quem posiciona o indicador alinha esse canto superior ao controle de destino, de
/// forma que a ponta do dedo caia em cima dele.
/// </summary>
public sealed class TapHintDrawable : IDrawable
{
    private const float TipX = 12f;
    private const float TipY = 3.2f;

    /// <summary>Preenchimento da mão. Branco para ler sobre qualquer réplica.</summary>
    public Color Fill { get; set; } = Color.FromArgb("#FCFCFC");

    /// <summary>Contorno da mão - o fundo escuro do app, para destacá-la da amostra atrás.</summary>
    public Color Outline { get; set; } = Color.FromArgb("#060B24");

    /// <summary>Anel de toque, no rosa da marca (#F27BEB).</summary>
    public Color Ripple { get; set; } = Color.FromArgb("#F27BEB");

    /// <summary>
    /// 0 a 1, avançado em laço pelo TapHintView. Comanda as duas coisas ao mesmo tempo: o quanto a
    /// mão está "pressionada" e o quanto os anéis já se abriram.
    /// </summary>
    public float Progress { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - size) / 2f,
            dirtyRect.Y + (dirtyRect.Height - size) / 2f);
        canvas.Scale(size / 24f, size / 24f);

        DrawRipples(canvas);

        // A mão desce e volta dentro do mesmo ciclo (seno de 0 a pi), imitando o toque em vez de
        // só piscar parada.
        var press = MathF.Sin(MathF.PI * Progress);
        canvas.SaveState();
        canvas.Translate(0f, 2.2f * press);
        DrawHand(canvas);
        canvas.RestoreState();

        canvas.RestoreState();
    }

    private void DrawRipples(ICanvas canvas)
    {
        // Dois anéis defasados em meio ciclo: sempre há um se abrindo, então o indicador nunca
        // fica um instante "morto" entre uma repetição e a seguinte.
        DrawRipple(canvas, Progress);
        DrawRipple(canvas, (Progress + 0.5f) % 1f);
    }

    private void DrawRipple(ICanvas canvas, float phase)
    {
        var radius = 2.4f + 6.2f * phase;
        var alpha = 0.75f * (1f - phase);
        if (alpha <= 0f)
            return;

        canvas.StrokeColor = Ripple.WithAlpha(alpha);
        canvas.StrokeSize = 1.4f;
        canvas.DrawCircle(TipX, TipY, radius);
    }

    private void DrawHand(ICanvas canvas)
    {
        // Silhueta em três peças que se sobrepõem: dedo indicador esticado, punho e polegar. Fica
        // legível a 34px, que é o tamanho em que o indicador é usado.
        var finger = BuildRoundedRect(10.4f, TipY, 3.2f, 11.2f, 1.6f);
        var fist = BuildRoundedRect(8.2f, 11.2f, 10.6f, 9.8f, 4.6f);
        var thumb = BuildRoundedRect(6.9f, 13.6f, 3.6f, 5.4f, 1.8f);

        // Contorno primeiro, preenchimento por cima: o traço escuro sobra só para fora da
        // silhueta, sem sujar o miolo branco nem marcar as emendas entre as três peças.
        canvas.StrokeColor = Outline;
        canvas.StrokeSize = 1.6f;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(finger);
        canvas.DrawPath(fist);
        canvas.DrawPath(thumb);

        canvas.FillColor = Fill;
        canvas.FillPath(finger);
        canvas.FillPath(fist);
        canvas.FillPath(thumb);
    }

    /// <summary>
    /// Retângulo de cantos arredondados montado à mão, por amostragem dos quartos de círculo -
    /// mesma escolha do FingerprintDrawable: não depender da convenção de ângulo/sentido de arco
    /// de cada rasterizador.
    /// </summary>
    private static PathF BuildRoundedRect(float x, float y, float width, float height, float radius)
    {
        var path = new PathF();
        var right = x + width;
        var bottom = y + height;

        path.MoveTo(x + radius, y);
        path.LineTo(right - radius, y);
        AppendQuarter(path, right - radius, y + radius, radius, -90f, 0f);
        path.LineTo(right, bottom - radius);
        AppendQuarter(path, right - radius, bottom - radius, radius, 0f, 90f);
        path.LineTo(x + radius, bottom);
        AppendQuarter(path, x + radius, bottom - radius, radius, 90f, 180f);
        path.LineTo(x, y + radius);
        AppendQuarter(path, x + radius, y + radius, radius, 180f, 270f);
        path.Close();

        return path;
    }

    private static void AppendQuarter(PathF path, float cx, float cy, float radius, float startDeg, float endDeg)
    {
        const int Steps = 6;
        var span = endDeg - startDeg;

        for (var i = 1; i <= Steps; i++)
        {
            var rad = (startDeg + span * i / Steps) * MathF.PI / 180f;
            path.LineTo(
                cx + radius * MathF.Cos(rad),
                cy + radius * MathF.Sin(rad));
        }
    }
}
