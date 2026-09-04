using GDSB.MAUI.Localization;

namespace GDSB.MAUI.Views;

public partial class FieldLabelView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(FieldLabelView), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty IsRequiredProperty = BindableProperty.Create(
        nameof(IsRequired), typeof(bool), typeof(FieldLabelView), false, propertyChanged: OnIsRequiredChanged);

    public static readonly BindableProperty HelpTopicIdProperty = BindableProperty.Create(
        nameof(HelpTopicId), typeof(string), typeof(FieldLabelView), null, propertyChanged: OnHelpTopicIdChanged);

    // S2325 é falso positivo, como em SelectableChip: GetValue/SetValue são métodos de instância
    // de BindableObject - uma bindable property não existe sem instância.
#pragma warning disable S2325
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Marca o campo com o asterisco de destaque. Só para o que a validação realmente cobra -
    /// campo opcional não recebe marca nenhuma, senão o asterisco perde o significado.
    /// </summary>
    public bool IsRequired
    {
        get => (bool)GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    /// <summary>Id de um tópico de HelpTopics.Ids. Vazio esconde o "?".</summary>
    public string? HelpTopicId
    {
        get => (string?)GetValue(HelpTopicIdProperty);
        set => SetValue(HelpTopicIdProperty, value);
    }
#pragma warning restore S2325

    public FieldLabelView()
    {
        InitializeComponent();
    }

    // oldValue é ignorado de propósito nos três callbacks, mas não dá pra remover: a assinatura é
    // fixa pelo delegate esperado por BindableProperty.Create. Mesmo caso de SelectableChip.
#pragma warning disable S1172
    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (FieldLabelView)bindable;
        view.TextLabel.Text = newValue as string ?? string.Empty;
        view.RefreshHelpSemantics();
    }

    private static void OnIsRequiredChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((FieldLabelView)bindable).RequiredMark.IsVisible = (bool)newValue;

    private static void OnHelpTopicIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (FieldLabelView)bindable;

        // O HelpButton já se esconde sozinho quando o TopicId é vazio - aqui só se repassa o valor.
        view.HelpAction.TopicId = newValue as string;
        view.RefreshHelpSemantics();
    }
#pragma warning restore S1172

    /// <summary>
    /// O "?" sozinho não diz nada para um leitor de tela - a descrição precisa citar o campo de
    /// que ele fala. Reavaliado a cada troca de Text (ver OnTextChanged) - como Text já chega
    /// traduzido via binding "{loc:Tr}", isso também mantém esta descrição em dia numa troca de
    /// idioma, sem assinar LanguageChanged à parte.
    /// </summary>
    private void RefreshHelpSemantics() =>
        SemanticProperties.SetDescription(HelpAction, LocalizationServiceLocator.Resolve().Format("A11y_HelpAbout", Text));
}
