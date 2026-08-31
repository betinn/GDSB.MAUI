using Microsoft.Maui.Graphics;

namespace GDSB.MAUI.Views;

/// <summary>
/// Desenha a marca GDSB como um cadeado que fecha, para confirmar a criação de um segredo.
///
/// A geometria é a mesma dos SVGs da marca (Resources/AppIcon/appiconfg.svg), num espaço
/// lógico de 128x128 que é escalado para o tamanho real do GraphicsView:
///
///   haste (arco do G) : círculo de centro (64,42) e raio 24, traço 11, pontas arredondadas
///   corpo (D deitado) : meio-disco de centro (64,64) e raio 42
///   fechadura         : círculo (64,79) raio 8 + rabo
///
/// O centro/raio da haste foram derivados do arco SVG "M83.2,27.6 A24,24 0 1 0 84.1,55.1":
/// a corda vai de (83.2,27.6) a (84.1,55.1), e com large-arc=1/sweep=0 o centro cai em
/// (64,42) - por isso o arco é percorrido no sentido negativo por ~290°, deixando a abertura
/// à direita, onde entra o rabo do G.
///
/// A fechadura é pintada com a cor do disco de fundo em vez de recortada por winding rule:
/// o resultado é idêntico sobre o disco opaco e não depende de EvenOdd no rasterizador de
/// cada plataforma.
/// </summary>
public sealed class LockSealDrawable : IDrawable
{
    // Marca
    private static readonly Color Pink = Color.FromArgb("#D936D3");
    private static readonly Color OffWhite = Color.FromArgb("#FCFCFC");
    private static readonly Color Navy = Color.FromArgb("#00186E");

    private const float ArcCenterX = 64f;
    private const float ArcCenterY = 42f;
    private const float ArcRadius = 24f;
    private const float ArcStroke = 11f;

    // Início e varredura do arco, em graus (sentido negativo = a abertura fica à direita).
    private const float ArcStartDeg = -36.87f;
    private const float ArcSweepDeg = -290.04f;

    // Altura de onde a haste cai até assentar no corpo.
    private const float ShackleTravel = 15f;

    /// <summary>0 = nada desenhado, 1 = cadeado fechado e assentado.</summary>
    public float Progress { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        var p = Math.Clamp(Progress, 0f, 1f);

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - size) / 2f,
            dirtyRect.Y + (dirtyRect.Height - size) / 2f);
        canvas.Scale(size / 128f, size / 128f);

        DrawBadge(canvas, p);
        DrawShackle(canvas, p);
        DrawBody(canvas, p);
        DrawRingPulse(canvas, p);

        canvas.RestoreState();
    }

    /// <summary>Disco navy que segura a marca, igual ao contêiner do ícone.</summary>
    private static void DrawBadge(ICanvas canvas, float p)
    {
        var t = EaseOutCubic(Segment(p, 0f, 0.22f));
        if (t <= 0f)
            return;

        var scale = 0.72f + 0.28f * t;

        canvas.SaveState();
        canvas.Alpha = t;
        ScaleAbout(canvas, 64f, 64f, scale);

        canvas.FillColor = Navy;
        canvas.FillCircle(64f, 64f, 60f);

        canvas.StrokeColor = Pink.WithAlpha(0.34f);
        canvas.StrokeSize = 1.6f;
        canvas.DrawCircle(64f, 64f, 60f);

        canvas.RestoreState();
    }

    /// <summary>
    /// A haste desce e "sela": o arco é traçado progressivamente enquanto cai, com um
    /// pequeno repique no fim (o estalo do cadeado).
    /// </summary>
    private static void DrawShackle(ICanvas canvas, float p)
    {
        var draw = Segment(p, 0.16f, 0.50f);
        if (draw <= 0f)
            return;

        var seat = EaseOutBack(Segment(p, 0.16f, 0.62f));
        var offsetY = -ShackleTravel * (1f - seat);

        canvas.SaveState();
        canvas.Translate(0f, offsetY);

        canvas.StrokeColor = Pink;
        canvas.StrokeSize = ArcStroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(BuildShackle(EaseOutCubic(draw)));

        canvas.RestoreState();
    }

    /// <summary>Corpo branco com a fechadura, que aparece quando a haste está quase assentada.</summary>
    private static void DrawBody(ICanvas canvas, float p)
    {
        var t = EaseOutCubic(Segment(p, 0.10f, 0.36f));
        if (t <= 0f)
            return;

        canvas.SaveState();
        canvas.Alpha = t;
        // Nasce um pouco achatado e cresce até o tamanho certo, como se assentasse.
        ScaleAbout(canvas, 64f, 64f, 0.86f + 0.14f * t);

        canvas.FillColor = OffWhite;
        canvas.FillPath(BuildBody());

        var keyhole = EaseOutCubic(Segment(p, 0.46f, 0.70f));
        if (keyhole > 0f)
        {
            canvas.Alpha = t * keyhole;
            canvas.FillColor = Navy;
            canvas.FillCircle(64f, 79f, 8f);

            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 6.5f;
            canvas.StrokeLineCap = LineCap.Round;

            var tail = new PathF();
            tail.MoveTo(64f, 86f);
            tail.CurveTo(57.5f, 89.5f, 57.5f, 96f, 64f, 99.5f);
            canvas.DrawPath(tail);
        }

        canvas.RestoreState();
    }

    /// <summary>Anel magenta que expande e some, marcando o instante em que trancou.</summary>
    private static void DrawRingPulse(ICanvas canvas, float p)
    {
        var t = Segment(p, 0.52f, 0.95f);
        if (t <= 0f || t >= 1f)
            return;

        var eased = EaseOutCubic(t);

        canvas.SaveState();
        canvas.Alpha = 0.5f * (1f - eased);
        canvas.StrokeColor = Pink;
        canvas.StrokeSize = 2.2f;
        canvas.DrawCircle(64f, 64f, 48f + 22f * eased);
        canvas.RestoreState();
    }

    /// <summary>Arco da haste, traçado de 0 a <paramref name="fraction"/> da varredura total.</summary>
    private static PathF BuildShackle(float fraction)
    {
        var path = new PathF();

        // 2° por segmento é suficiente: com traço 11 e pontas redondas a poligonal não se nota.
        var steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(ArcSweepDeg) * fraction / 2f));

        for (var i = 0; i <= steps; i++)
        {
            var deg = ArcStartDeg + ArcSweepDeg * fraction * i / steps;
            var rad = deg * MathF.PI / 180f;
            var x = ArcCenterX + ArcRadius * MathF.Cos(rad);
            var y = ArcCenterY + ArcRadius * MathF.Sin(rad);

            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        // O rabo do G só entra quando o arco já fechou por inteiro.
        if (fraction >= 0.999f)
        {
            path.LineTo(84.1f, 45f);
            path.LineTo(71f, 45f);
        }

        return path;
    }

    /// <summary>Meio-disco inferior: "M22,64 h84 a42,42 0 0 1 -84,0 z".</summary>
    private static PathF BuildBody()
    {
        var path = new PathF();
        path.MoveTo(22f, 64f);
        path.LineTo(106f, 64f);

        const int steps = 48;
        for (var i = 1; i <= steps; i++)
        {
            var rad = MathF.PI * i / steps; // 0 -> pi, passando por baixo (y cresce para baixo)
            path.LineTo(64f + 42f * MathF.Cos(rad), 64f + 42f * MathF.Sin(rad));
        }

        path.Close();
        return path;
    }

    private static void ScaleAbout(ICanvas canvas, float cx, float cy, float scale)
    {
        canvas.Translate(cx, cy);
        canvas.Scale(scale, scale);
        canvas.Translate(-cx, -cy);
    }

    /// <summary>Recorta [from,to] do progresso global e devolve 0..1 dentro dessa janela.</summary>
    private static float Segment(float p, float from, float to)
        => Math.Clamp((p - from) / (to - from), 0f, 1f);

    private static float EaseOutCubic(float t)
    {
        var u = 1f - t;
        return 1f - u * u * u;
    }

    /// <summary>Passa um pouco do ponto final e volta - é o repique da haste ao trancar.</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.1f;
        const float c3 = c1 + 1f;
        var u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
