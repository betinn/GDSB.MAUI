using CommunityToolkit.Mvvm.Messaging;
using GDSB.MAUI.Help;
using GDSB.MAUI.Localization;

namespace GDSB.MAUI.Views;

/// <summary>
/// O "?" que abre o painel de ajuda. Existe como controle próprio, e não como um Button com um
/// Clicked em cada code-behind, porque ele aparece em quatro telas - repetir a publicação da
/// mensagem em cada uma seria o mesmo bloco copiado quatro vezes.
///
/// Subclasse de Button (como SelectableChip) para herdar o HelpButtonStyle, que por isso precisa
/// de ApplyToDerivedTypes="True".
/// </summary>
public class HelpButton : Button
{
    public static readonly BindableProperty TopicIdProperty = BindableProperty.Create(
        nameof(TopicId), typeof(string), typeof(HelpButton), null, propertyChanged: OnTopicIdChanged);

    // S2325 é falso positivo, como em SelectableChip: GetValue/SetValue são métodos de instância
    // de BindableObject - uma bindable property não existe sem instância.
#pragma warning disable S2325
    /// <summary>Id de um tópico de <see cref="HelpTopics.Ids"/>. Vazio esconde o botão.</summary>
    public string? TopicId
    {
        get => (string?)GetValue(TopicIdProperty);
        set => SetValue(TopicIdProperty, value);
    }
#pragma warning restore S2325

    public HelpButton()
    {
        IsVisible = false;
        // Setado uma vez, na construção - resolvido fora da DI porque o XAML instancia este
        // controle direto (ver LocalizationServiceLocator). Não reage a uma troca de idioma
        // durante a vida do botão: é texto só de leitor de tela, e o "?" visível não muda.
        SemanticProperties.SetDescription(this, LocalizationServiceLocator.Resolve().Get("A11y_HelpButtonDescription"));
        Clicked += OnClicked;
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(TopicId))
            WeakReferenceMessenger.Default.Send(new HelpRequestedMessage(TopicId));
    }

    // oldValue é ignorado de propósito, mas não dá pra remover: a assinatura é fixa pelo delegate
    // esperado por BindableProperty.Create. Mesmo caso de SelectableChip.
#pragma warning disable S1172
    private static void OnTopicIdChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((HelpButton)bindable).IsVisible = !string.IsNullOrEmpty(newValue as string);
#pragma warning restore S1172
}
