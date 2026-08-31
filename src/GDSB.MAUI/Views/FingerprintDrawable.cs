using Microsoft.Maui.Graphics;

namespace GDSB.MAUI.Views;

/// <summary>
/// Ícone de impressão digital, no lugar do emoji 🔐 que o app usava para biometria - emoji
/// muda de desenho a cada plataforma e não aceita a cor da marca.
///
/// Os traços vêm do protótipo (viewBox 24x24), aqui escalados para o tamanho da view. As
/// cristas são arcos concêntricos em (12,11) com raios 1,8 / 4,6 / 7,8, ligados por curvas
/// cúbicas - os arcos SVG originais eram "a1.8 1.8 0 0 1 3.6 0" e equivalentes, cujos centros
/// caem todos nesse mesmo ponto.
///
/// Como no restante do app, os arcos são traçados por amostragem em vez das APIs de arco do
/// canvas: evita depender da convenção de ângulo/sentido de cada rasterizador.
/// </summary>
public sealed class FingerprintDrawable : IDrawable
{
    private const float CenterX = 12f;
    private const float CenterY = 11f;

    /// <summary>Cor do traço. O card de biometria usa #F27BEB; o botão, branco.</summary>
    public Color Stroke { get; set; } = Color.FromArgb("#F27BEB");

    /// <summary>Espessura no espaço 24x24 do desenho: 1,5 no card e 1,7 no botão.</summary>
    public float StrokeWidth { get; set; } = 1.5f;

    /// <summary>
    /// Omite a crista curta da esquerda, como o protótipo faz no botão: em 18px ela vira
    /// um risco solto e só suja o desenho.
    /// </summary>
    public bool Compact { get; set; }

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

        canvas.StrokeColor = Stroke;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        canvas.DrawPath(Compact ? CompactRidges : FullRidges);

        canvas.RestoreState();
    }

    private static readonly PathF FullRidges = BuildRidges(compact: false);
    private static readonly PathF CompactRidges = BuildRidges(compact: true);

    private static PathF BuildRidges(bool compact)
    {
        var path = new PathF();

        // Crista central, a mais curta.
        path.MoveTo(12f, 11f);
        path.CurveTo(12f, 14.5f, 11.5f, 16.8f, 10.6f, 18.6f);

        // Segunda crista: sobe pela esquerda, contorna o topo com raio 1,8 e desce.
        path.MoveTo(8.6f, 19.5f);
        path.CurveTo(9.7f, 17.3f, 10.2f, 14.6f, 10.2f, 11f);
        AppendArc(path, 1.8f, 180f, 360f);
        path.CurveTo(13.8f, 13.6f, 13.5f, 15.7f, 13f, 17.4f);

        // Terceira crista: mesmo desenho espelhado, raio 4,6.
        path.MoveTo(15.6f, 18.2f);
        path.CurveTo(16.3f, 16.1f, 16.6f, 13.6f, 16.6f, 11f);
        AppendArc(path, 4.6f, 0f, -180f);
        path.CurveTo(7.4f, 12.5f, 7.3f, 13.9f, 7f, 15.1f);

        // Crista externa, raio 7,8, incompleta à esquerda.
        path.MoveTo(19.4f, 15.6f);
        path.CurveTo(19.7f, 14f, 19.8f, 12.4f, 19.8f, 11f);
        AppendArc(path, 7.8f, 0f, -133.95f);

        if (!compact)
        {
            path.MoveTo(4.2f, 13.4f);
            path.CurveTo(4.3f, 12.6f, 4.4f, 11.8f, 4.4f, 11f);
            path.CurveTo(4.4f, 9.6f, 4.8f, 8.3f, 5.4f, 7.2f);
        }

        return path;
    }

    /// <summary>Acrescenta um arco centrado em (12,11) ao subcaminho atual, por amostragem.</summary>
    private static void AppendArc(PathF path, float radius, float startDeg, float endDeg)
    {
        var span = endDeg - startDeg;
        var steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(span) / 6f));

        for (var i = 1; i <= steps; i++)
        {
            var rad = (startDeg + span * i / steps) * MathF.PI / 180f;
            path.LineTo(
                CenterX + radius * MathF.Cos(rad),
                CenterY + radius * MathF.Sin(rad));
        }
    }
}
