using CommunityToolkit.Mvvm.Messaging;
using GDSB.MAUI.Help;

namespace GDSB.MAUI.Views;

public partial class HelpSheetView : ContentView
{
    public HelpSheetView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Passa a atender os pedidos de ajuda publicados pelos botões "?". Chamado no OnAppearing da
    /// página que hospeda o painel, e desfeito no OnDisappearing: com Shell, a página anterior
    /// continua carregada na pilha de navegação, então amarrar isso a Loaded/Unloaded faria o
    /// painel de uma tela que não está à vista responder ao "?" de outra.
    /// </summary>
    public void StartListening()
    {
        // Registrar duas vezes o mesmo destinatário lança no WeakReferenceMessenger, e há
        // caminhos de navegação em que OnAppearing roda sem um OnDisappearing antes - desfazer
        // primeiro (operação idempotente) é mais barato do que confiar no pareamento.
        StopListening();

        WeakReferenceMessenger.Default.Register<HelpRequestedMessage>(
            this, static (recipient, message) => ((HelpSheetView)recipient).Show(message.TopicId));
    }

    public void StopListening() => WeakReferenceMessenger.Default.Unregister<HelpRequestedMessage>(this);

    public void Show(string topicId)
    {
        if (!HelpTopics.TryGet(topicId, out var topic))
        {
#if DEBUG
            throw new InvalidOperationException(
                $"Tópico de ajuda '{topicId}' não existe em HelpTopics. Um \"?\" do XAML está apontando para um id que ninguém declarou.");
#else
            return;
#endif
        }

        TitleLabel.Text = topic.Title;

        BlocksHost.Clear();
        foreach (var block in topic.Blocks)
            BlocksHost.Add(CreateBlockView(block));

        Overlay.IsVisible = true;
    }

    public void Hide()
    {
        Overlay.IsVisible = false;

        // Solta as réplicas (e as animações da mãozinha dentro delas) em vez de deixá-las vivas
        // atrás de um IsVisible=False até a próxima abertura.
        BlocksHost.Clear();
    }

    private void OnDismissClicked(object? sender, EventArgs e) => Hide();

    private static View CreateBlockView(HelpBlock block) => block.Kind switch
    {
        HelpBlockKind.Heading => new Label { Text = block.Value, Style = FindStyle("HelpHeadingStyle") },
        HelpBlockKind.Visual => CreateVisualView(block),
        _ => new Label { Text = block.Value, Style = FindStyle("HelpTextStyle") },
    };

    private static View CreateVisualView(HelpBlock block)
    {
        var caption = new Label { Text = block.Caption ?? string.Empty, Style = FindStyle("HelpVisualCaptionStyle") };
        var sample = ResolveVisual(block.Value);

        if (sample is null)
            return caption;

        var frame = new Border { Style = FindStyle("HelpVisualFrameStyle"), Content = sample };

        return new VerticalStackLayout { Spacing = 8, Children = { frame, caption } };
    }

    /// <summary>
    /// Resolve a chave da amostra no dicionário Resources/HelpVisuals.xaml, agregado em App.xaml.
    /// O projeto de teste é net10.0 puro e não enxerga XAML, então esta correspondência não pode
    /// ser coberta por teste: em DEBUG ela falha alto, para uma chave sem template não passar
    /// despercebida como um quadro em branco no meio do painel.
    /// </summary>
    private static View? ResolveVisual(string visualId)
    {
        if (Application.Current?.Resources.TryGetValue(visualId, out var resource) == true
            && resource is DataTemplate template
            && template.CreateContent() is View view)
        {
            return view;
        }

#if DEBUG
        throw new InvalidOperationException(
            $"Amostra visual '{visualId}' não tem DataTemplate em Resources/HelpVisuals.xaml.");
#else
        return null;
#endif
    }

    private static Style? FindStyle(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Style style
            ? style
            : null;
}
