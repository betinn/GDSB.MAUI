using Microsoft.Maui.Graphics;

namespace GDSB.MAUI.Views;

/// <summary>
/// A marca GDSB parada, para uso decorativo na interface (o emblema da tela de desbloqueio).
/// Reaproveita a geometria do <see cref="LockSealDrawable"/> em vez de duplicá-la, então a
/// marca da tela e a das animações não podem divergir.
/// </summary>
public sealed class BrandMarkDrawable : IDrawable
{
    /// <summary>Desenha o disco navy atrás da marca. Desligue sobre superfície já escura.</summary>
    public bool ShowBadge { get; set; } = true;

    public void Draw(ICanvas canvas, RectF dirtyRect)
        => LockSealDrawable.DrawStaticMark(canvas, dirtyRect, ShowBadge);
}

/// <summary>Qual momento o selo está confirmando.</summary>
public enum LockSealMode
{
    /// <summary>Item novo: a marca se monta e a haste trava.</summary>
    Create,

    /// <summary>Item existente alterado: o anel fecha a volta e a marca pulsa.</summary>
    Update,

    /// <summary>Item removido: a marca racha e estilhaça.</summary>
    Delete,
}

/// <summary>
/// Desenha a marca GDSB animada, para confirmar criação, edição e exclusão de um segredo.
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
/// Arcos são traçados por amostragem em vez das APIs de arco do canvas: com traço grosso e
/// pontas redondas a poligonal não se nota, e evita depender da convenção de ângulo/sentido
/// de cada rasterizador.
///
/// A fechadura é pintada com a cor do disco de fundo em vez de recortada por winding rule:
/// o resultado é idêntico sobre o disco opaco e não depende de EvenOdd no rasterizador de
/// cada plataforma.
/// </summary>
public sealed class LockSealDrawable : IDrawable
{
    private static readonly Color Pink = Color.FromArgb("#D936D3");
    private static readonly Color PinkLight = Color.FromArgb("#F27BEB");
    private static readonly Color OffWhite = Color.FromArgb("#FCFCFC");
    private static readonly Color Navy = Color.FromArgb("#00186E");
    private static readonly Color Danger = Color.FromArgb("#FF5A6A");
    private static readonly Color Muted = Color.FromArgb("#6E77A8");

    private const float ArcCenterX = 64f;
    private const float ArcCenterY = 42f;
    private const float ArcRadius = 24f;
    private const float ArcStroke = 11f;

    // Início e varredura do arco, em graus (sentido negativo = a abertura fica à direita).
    private const float ArcStartDeg = -36.87f;
    private const float ArcSweepDeg = -290.04f;

    // Altura de onde a haste cai até assentar no corpo, na criação.
    private const float ShackleTravel = 15f;

    // Origem da explosão e raio que separa o miolo da borda, na exclusão.
    private const float BurstX = 64f;
    private const float BurstY = 60f;
    private const float BurstMidRadius = 36f;

    /// <summary>0 = início da animação, 1 = fim.</summary>
    public float Progress { get; set; }

    /// <summary>Qual das três animações desenhar.</summary>
    public LockSealMode Mode { get; set; } = LockSealMode.Create;

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

        switch (Mode)
        {
            case LockSealMode.Update:
                DrawUpdateSeal(canvas, p);
                break;
            case LockSealMode.Delete:
                DrawDeleteSeal(canvas, p);
                break;
            default:
                DrawCreateSeal(canvas, p);
                break;
        }

        canvas.RestoreState();
    }

    /// <summary>
    /// A marca completa e parada, no mesmo enquadramento das animações. Usada pelo
    /// <see cref="BrandMarkDrawable"/>.
    /// </summary>
    internal static void DrawStaticMark(ICanvas canvas, RectF dirtyRect, bool withBadge)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - size) / 2f,
            dirtyRect.Y + (dirtyRect.Height - size) / 2f);
        canvas.Scale(size / 128f, size / 128f);

        if (withBadge)
            DrawBadge(canvas, 1f, 1f, Pink, 0.34f);

        DrawMarkFull(canvas);
        canvas.RestoreState();
    }

    // ---------------------------------------------------------------- criação

    private static void DrawCreateSeal(ICanvas canvas, float p)
    {
        var badge = EaseOutCubic(Segment(p, 0f, 0.22f));
        if (badge > 0f)
            DrawBadge(canvas, badge, 0.72f + 0.28f * badge, Pink, 0.34f);

        // A haste desce e "sela": o arco é traçado progressivamente enquanto cai, com um
        // pequeno repique no fim (o estalo do cadeado).
        var draw = Segment(p, 0.16f, 0.50f);
        if (draw > 0f)
        {
            var seat = EaseOutBack(Segment(p, 0.16f, 0.62f));

            canvas.SaveState();
            canvas.Translate(0f, -ShackleTravel * (1f - seat));
            SetShackleStroke(canvas, Pink);
            canvas.DrawPath(BuildShackle(EaseOutCubic(draw)));
            canvas.RestoreState();
        }

        var body = EaseOutCubic(Segment(p, 0.10f, 0.36f));
        if (body > 0f)
        {
            canvas.SaveState();
            canvas.Alpha = body;
            // Nasce um pouco achatado e cresce até o tamanho certo, como se assentasse.
            ScaleAbout(canvas, 64f, 64f, 0.86f + 0.14f * body);

            canvas.FillColor = OffWhite;
            canvas.FillPath(BodyPath);

            var keyhole = EaseOutCubic(Segment(p, 0.46f, 0.70f));
            if (keyhole > 0f)
            {
                canvas.Alpha = body * keyhole;
                DrawKeyhole(canvas, Navy);
            }

            canvas.RestoreState();
        }

        // Anel magenta que expande e some, marcando o instante em que trancou.
        var ring = Segment(p, 0.52f, 0.95f);
        if (ring > 0f && ring < 1f)
        {
            var eased = EaseOutCubic(ring);
            canvas.SaveState();
            canvas.Alpha = 0.5f * (1f - eased);
            canvas.StrokeColor = Pink;
            canvas.StrokeSize = 2.2f;
            canvas.DrawCircle(64f, 64f, 48f + 22f * eased);
            canvas.RestoreState();
        }
    }

    // ----------------------------------------------------------------- edição

    /// <summary>
    /// O cadeado nunca se abre: o segredo já existia e continua trancado - o que mudou foi o
    /// conteúdo. Um anel percorre 360° e, ao fechar, a marca pulsa e a fechadura acende.
    /// </summary>
    private static void DrawUpdateSeal(ICanvas canvas, float p)
    {
        var appear = EaseOutCubic(Segment(p, 0f, 0.14f));
        if (appear <= 0f)
            return;

        DrawBadge(canvas, appear, 0.94f + 0.06f * appear, Pink, 0.34f);

        canvas.SaveState();
        canvas.Alpha = appear;
        // Pulso curto no instante em que o anel fecha.
        ScaleAbout(canvas, 64f, 64f, 1f + 0.055f * MathF.Sin(MathF.PI * Segment(p, 0.58f, 0.86f)));

        SetShackleStroke(canvas, Pink);
        canvas.DrawPath(FullShacklePath);

        canvas.FillColor = OffWhite;
        canvas.FillPath(BodyPath);

        DrawKeyhole(canvas, Navy);

        var flash = MathF.Sin(MathF.PI * Segment(p, 0.55f, 0.90f));
        if (flash > 0f)
        {
            canvas.Alpha = appear * flash * 0.85f;
            DrawKeyhole(canvas, PinkLight);
        }

        canvas.RestoreState();

        var sweep = EaseOutCubic(Segment(p, 0.10f, 0.62f));
        if (sweep > 0f)
        {
            canvas.SaveState();
            // Depois de fechar a volta, o anel desaparece.
            canvas.Alpha = appear * (sweep >= 1f ? 1f - EaseOutCubic(Segment(p, 0.72f, 1f)) : 1f);
            canvas.StrokeColor = PinkLight;
            canvas.StrokeSize = 3f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawPath(BuildArc(64f, 64f, 54f, -90f, 360f * sweep));
            canvas.RestoreState();
        }

        var ripple = Segment(p, 0.62f, 1f);
        if (ripple > 0f && ripple < 1f)
        {
            var eased = EaseOutCubic(ripple);
            canvas.SaveState();
            canvas.Alpha = 0.42f * (1f - eased);
            canvas.StrokeColor = PinkLight;
            canvas.StrokeSize = 2f;
            canvas.DrawCircle(64f, 64f, 54f + 16f * eased);
            canvas.RestoreState();
        }
    }

    // -------------------------------------------------------------- exclusão

    /// <summary>
    /// A marca treme, racha e se parte em cacos de tamanhos diferentes, cada um com rumo,
    /// giro e gravidade próprios. Termina com a tela vazia.
    /// </summary>
    private static void DrawDeleteSeal(ICanvas canvas, float p)
    {
        var appear = EaseOutCubic(Segment(p, 0f, 0.10f));
        var burstT = Segment(p, 0.20f, 0.94f);
        var burst = EaseOutCubic(burstT);

        // O disco sai cedo: com o miolo estilhaçado, um navy sólido no fundo vira um buraco.
        var badge = appear * (1f - EaseOutCubic(Segment(p, 0.18f, 0.44f)));
        if (badge > 0.002f)
            DrawBadge(canvas, badge, 1f + 0.07f * EaseOutCubic(Segment(p, 0.15f, 0.32f)), Danger, 0.45f);

        if (burstT <= 0f)
        {
            DrawTensionPhase(canvas, p, appear);
        }
        else if (burstT < 1f)
        {
            DrawShards(canvas, burstT, burst, appear);
        }

        // Sem clarão: um círculo sólido com alpha baixo sobre fundo escuro vira mancha, não
        // brilho - precisaria de composição aditiva. A onda de choque e os cacos já bastam.

        var shock = Segment(p, 0.21f, 0.60f);
        if (shock > 0f && shock < 1f)
        {
            var eased = EaseOutCubic(shock);
            canvas.SaveState();
            canvas.Alpha = 0.6f * (1f - eased);
            canvas.StrokeColor = Danger;
            canvas.StrokeSize = 3.4f * (1f - eased) + 0.8f;
            // Nasce já fora da marca, para não cortar o desenho.
            canvas.DrawCircle(BurstX, BurstY, 34f + 68f * eased);
            canvas.RestoreState();
        }

        var debris = Segment(p, 0.20f, 1f);
        if (debris > 0f && debris < 1f)
        {
            var eased = EaseOutCubic(debris);
            canvas.SaveState();
            canvas.FillColor = Muted;
            canvas.Alpha = 0.8f * (1f - eased);

            foreach (var d in DebrisTable)
            {
                var rad = d.AngleDeg * MathF.PI / 180f;
                var dist = eased * 104f * d.Speed;
                canvas.FillCircle(
                    BurstX + dist * MathF.Cos(rad),
                    BurstY + dist * MathF.Sin(rad) + 56f * d.Gravity * eased * eased,
                    Math.Max(0.3f, d.Radius * (1f - 0.7f * eased)));
            }

            canvas.RestoreState();
        }
    }

    /// <summary>Antes de partir: a marca treme e as fraturas abrem.</summary>
    private static void DrawTensionPhase(ICanvas canvas, float p, float appear)
    {
        var shake = EaseOutCubic(Segment(p, 0.02f, 0.20f));

        canvas.SaveState();
        canvas.Alpha = appear;
        canvas.Translate(MathF.Sin(p * 95f) * 2.4f * shake, MathF.Sin(p * 74f) * 1.5f * shake);

        DrawMarkFull(canvas);

        var crack = Segment(p, 0.11f, 0.20f);
        if (crack > 0f)
        {
            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.Alpha = appear * crack * 0.85f;

            // As fraturas não chegam ao centro: 16 linhas convergindo viravam um borrão.
            // Todas num único PathF: um por linha eram 16 alocações por frame.
            var cracks = new PathF();
            foreach (var s in Shards)
            {
                var rad = s.StartDeg * MathF.PI / 180f;
                var inner = Math.Max(s.InnerRadius, 9f);
                var outer = Math.Min(s.OuterRadius, 58f);
                var tip = inner + (outer - inner) * crack;

                cracks.MoveTo(BurstX + inner * MathF.Cos(rad), BurstY + inner * MathF.Sin(rad));
                cracks.LineTo(BurstX + tip * MathF.Cos(rad), BurstY + tip * MathF.Sin(rad));
            }

            canvas.DrawPath(cracks);

            // A fratura circular que separa o miolo da borda.
            canvas.Alpha = appear * crack * 0.5f;
            canvas.StrokeSize = 1.2f;
            canvas.DrawCircle(BurstX, BurstY, BurstMidRadius);
        }

        canvas.RestoreState();
    }

    /// <summary>
    /// Cada caco é a marca de verdade recortada num setor: aplica-se a transformação do caco,
    /// recorta-se pelo setor e desenha-se a marca inteira. Como recorte e desenho recebem a
    /// mesma transformação, o visível é exatamente aquele pedaço deslocado.
    /// </summary>
    private static void DrawShards(ICanvas canvas, float burstT, float burst, float appear)
    {
        for (var i = 0; i < Shards.Length; i++)
        {
            var s = Shards[i];

            var alpha = appear * (1f - EaseInCubic(Segment(burstT, 0f, s.Life)));
            if (alpha <= 0.004f)
                continue; // caco já apagado: nada a desenhar

            var rad = (s.MidDeg + s.Deviation) * MathF.PI / 180f;
            var midRad = s.MidDeg * MathF.PI / 180f;

            var dx = burst * 80f * s.Speed * MathF.Cos(rad) + s.Drift * burst;
            var dy = burst * 80f * s.Speed * MathF.Sin(rad) + 62f * s.Gravity * burst * burst;

            // Gira em torno do próprio centro aproximado, não da origem da explosão.
            var pivot = (s.InnerRadius + Math.Min(s.OuterRadius, 60f)) / 2f;
            var cx = BurstX + pivot * MathF.Cos(midRad);
            var cy = BurstY + pivot * MathF.Sin(midRad);

            canvas.SaveState();
            canvas.Alpha = alpha;
            canvas.Translate(dx, dy);
            canvas.Rotate(s.Spin * burst, cx, cy);

            var shrink = 1f - s.Shrink * burst;
            canvas.Translate(cx, cy);
            canvas.Scale(shrink, shrink);
            canvas.Translate(-cx, -cy);

            canvas.ClipPath(SectorPaths[i], WindingMode.NonZero);
            DrawMarkFull(canvas);
            canvas.RestoreState();
        }
    }

    // ------------------------------------------------------------ tabela dos cacos

    private readonly record struct Shard(
        float InnerRadius, float OuterRadius, float StartDeg, float EndDeg, float MidDeg,
        float Deviation, float Speed, float Gravity, float Spin, float Drift, float Life, float Shrink);

    private readonly record struct DebrisBit(float AngleDeg, float Speed, float Gravity, float Radius);

    private static readonly Shard[] Shards = BuildShards();
    private static readonly DebrisBit[] DebrisTable = BuildDebris();

    // Geometria que não muda de frame para frame, construída uma vez. Reconstruir estes
    // caminhos a cada frame custava ~238 KB por frame só na exclusão (16 cacos, cada um
    // redesenhando a marca inteira) - lixo suficiente para o coletor pausar no meio da
    // animação e ela parecer travada.
    private static readonly PathF FullShacklePath = BuildShackle(1f);
    private static readonly PathF BodyPath = BuildBody();
    private static readonly PathF KeyholeTailPath = BuildKeyholeTail();
    private static readonly PathF[] SectorPaths = BuildSectorPaths();

    /// <summary>
    /// Sorteio determinístico (não Random): a animação roda igual toda vez, o que a torna
    /// testável. Em double de propósito - em float a precisão do produto grande muda a parte
    /// fracionária e os valores deixariam de bater com o protótipo.
    /// </summary>
    private static float Hash(int i, int salt)
    {
        var x = Math.Sin(i * 127.1 + salt * 311.7) * 43758.5453;
        return (float)(x - Math.Floor(x));
    }

    /// <summary>
    /// Duas faixas radiais: o miolo quebra em 5 pedaços grandes e a borda em 11 lascas finas,
    /// então os cacos têm tamanhos claramente diferentes. Os passos somam 360° em cada faixa.
    /// </summary>
    private static Shard[] BuildShards()
    {
        (float Inner, float Outer, int[] Steps)[] bands =
        [
            (0f, BurstMidRadius, [70, 62, 78, 66, 84]),
            (BurstMidRadius, 250f, [34, 28, 41, 25, 37, 30, 44, 26, 33, 29, 33]),
        ];

        var shards = new List<Shard>();
        var id = 0;

        foreach (var band in bands)
        {
            var angle = -180f;
            foreach (var step in band.Steps)
            {
                var start = angle;
                var end = angle + step;
                angle = end;

                shards.Add(new Shard(
                    band.Inner, band.Outer, start, end, (start + end) / 2f,
                    Deviation: (Hash(id, 1) - 0.5f) * 74f,
                    Speed: 0.42f + Hash(id, 2) * 1.22f,
                    Gravity: 0.45f + Hash(id, 3) * 1.45f,
                    Spin: (Hash(id, 4) - 0.5f) * 300f,
                    Drift: (Hash(id, 5) - 0.5f) * 42f,
                    Life: 0.70f + Hash(id, 6) * 0.30f,
                    Shrink: 0.18f + Hash(id, 7) * 0.30f));

                id++;
            }
        }

        return [.. shards];
    }

    private static DebrisBit[] BuildDebris()
    {
        var bits = new DebrisBit[22];
        for (var i = 0; i < bits.Length; i++)
        {
            bits[i] = new DebrisBit(
                Hash(i, 11) * 360f - 180f,
                0.4f + Hash(i, 12) * 1.3f,
                0.5f + Hash(i, 13) * 1.5f,
                1.3f + Hash(i, 14) * 2.2f);
        }

        return bits;
    }

    // ------------------------------------------------------------------ formas

    /// <summary>Disco navy que segura a marca, igual ao contêiner do ícone.</summary>
    private static void DrawBadge(ICanvas canvas, float alpha, float scale, Color ring, float ringAlpha)
    {
        canvas.SaveState();
        canvas.Alpha = alpha;
        ScaleAbout(canvas, 64f, 64f, scale);

        canvas.FillColor = Navy;
        canvas.FillCircle(64f, 64f, 60f);

        canvas.Alpha = alpha * ringAlpha;
        canvas.StrokeColor = ring;
        canvas.StrokeSize = 1.6f;
        canvas.DrawCircle(64f, 64f, 60f);

        canvas.RestoreState();
    }

    /// <summary>A marca completa e assentada - a base dos cacos e do selo de edição.</summary>
    private static void DrawMarkFull(ICanvas canvas)
    {
        SetShackleStroke(canvas, Pink);
        canvas.DrawPath(FullShacklePath);

        canvas.FillColor = OffWhite;
        canvas.FillPath(BodyPath);

        DrawKeyhole(canvas, Navy);
    }

    private static void DrawKeyhole(ICanvas canvas, Color color)
    {
        canvas.FillColor = color;
        canvas.FillCircle(64f, 79f, 8f);

        canvas.StrokeColor = color;
        canvas.StrokeSize = 6.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(KeyholeTailPath);
    }

    private static PathF BuildKeyholeTail()
    {
        var tail = new PathF();
        tail.MoveTo(64f, 86f);
        tail.CurveTo(57.5f, 89.5f, 57.5f, 96f, 64f, 99.5f);
        return tail;
    }

    private static PathF[] BuildSectorPaths()
    {
        var paths = new PathF[Shards.Length];
        for (var i = 0; i < paths.Length; i++)
            paths[i] = BuildSector(Shards[i]);
        return paths;
    }

    private static void SetShackleStroke(ICanvas canvas, Color color)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = ArcStroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
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

    /// <summary>Arco aberto, para o anel que varre a volta na edição.</summary>
    private static PathF BuildArc(float cx, float cy, float radius, float startDeg, float sweepDeg)
    {
        var path = new PathF();
        var steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(sweepDeg) / 4f));

        for (var i = 0; i <= steps; i++)
        {
            var rad = (startDeg + sweepDeg * i / steps) * MathF.PI / 180f;
            var x = cx + radius * MathF.Cos(rad);
            var y = cy + radius * MathF.Sin(rad);

            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        return path;
    }

    /// <summary>Setor anelar que recorta um caco. Com raio interno 0 vira uma fatia comum.</summary>
    private static PathF BuildSector(Shard s)
    {
        var path = new PathF();
        var span = s.EndDeg - s.StartDeg;
        var steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(span) / 4f));

        var startRad = s.StartDeg * MathF.PI / 180f;
        var hasHole = s.InnerRadius >= 0.01f;

        if (hasHole)
        {
            path.MoveTo(
                BurstX + s.InnerRadius * MathF.Cos(startRad),
                BurstY + s.InnerRadius * MathF.Sin(startRad));
            path.LineTo(
                BurstX + s.OuterRadius * MathF.Cos(startRad),
                BurstY + s.OuterRadius * MathF.Sin(startRad));
        }
        else
        {
            path.MoveTo(BurstX, BurstY);
        }

        for (var i = 0; i <= steps; i++)
        {
            var rad = (s.StartDeg + span * i / steps) * MathF.PI / 180f;
            path.LineTo(
                BurstX + s.OuterRadius * MathF.Cos(rad),
                BurstY + s.OuterRadius * MathF.Sin(rad));
        }

        if (hasHole)
        {
            for (var i = steps; i >= 0; i--)
            {
                var rad = (s.StartDeg + span * i / steps) * MathF.PI / 180f;
                path.LineTo(
                    BurstX + s.InnerRadius * MathF.Cos(rad),
                    BurstY + s.InnerRadius * MathF.Sin(rad));
            }
        }

        path.Close();
        return path;
    }

    // ----------------------------------------------------------------- auxiliares

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

    private static float EaseInCubic(float t) => t * t * t;

    /// <summary>Passa um pouco do ponto final e volta - é o repique da haste ao trancar.</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.1f;
        const float c3 = c1 + 1f;
        var u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
