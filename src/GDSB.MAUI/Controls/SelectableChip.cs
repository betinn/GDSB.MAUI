using Microsoft.Maui.Graphics;

namespace GDSB.MAUI.Controls
{
    // Botão de chip cujo destaque de "selecionado" é ligado por IsSelected, em vez de repetir
    // Button.Triggers/DataTrigger/3 Setters em cada chip do XAML - esse bloco (~7 linhas) se
    // repetia dezenas de vezes entre VaultPage, VaultSettingsPage e CreateVaultPage, sinalizado
    // como duplicação pelo Sonar. ClearValue devolve o controle pro que o FilterChipStyle já
    // define (a Style precisa de ApplyToDerivedTypes="True" pra valer aqui, já que o TargetType
    // dela é Button) - SetDynamicResource/RemoveDynamicResource de BindableObject não servem
    // porque são internos ao assembly do MAUI, não públicos.
    public class SelectableChip : Button
    {
        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected), typeof(bool), typeof(SelectableChip), false, propertyChanged: OnIsSelectedChanged);

        // S2325 é falso positivo: GetValue/SetValue são métodos de instância de BindableObject -
        // uma bindable property não existe sem instância, então isto não pode virar static.
#pragma warning disable S2325
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
#pragma warning restore S2325

        // oldValue é ignorado de propósito, mas não dá pra remover: a assinatura é fixa pelo
        // delegate BindablePropertyChangedDelegate esperado por BindableProperty.Create.
#pragma warning disable S1172
        private static void OnIsSelectedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var chip = (SelectableChip)bindable;
            if ((bool)newValue)
            {
                chip.BackgroundColor = ResourceColor("Primary");
                chip.BorderColor = ResourceColor("Primary");
                chip.TextColor = ResourceColor("White");
            }
            else
            {
                chip.ClearValue(BackgroundColorProperty);
                chip.ClearValue(BorderColorProperty);
                chip.ClearValue(TextColorProperty);
            }
        }
#pragma warning restore S1172

        private static Color ResourceColor(string key) =>
            Application.Current?.Resources.TryGetValue(key, out var value) is true && value is Color color
                ? color
                : Colors.Transparent;
    }
}
