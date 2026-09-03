using System.Globalization;

namespace GDSB.MAUI.Converters
{
    // Compara o valor bindado com o ConverterParameter do chip (SelectableChip.IsSelected) -
    // sempre como string, porque o parâmetro sempre chega como string do XAML e o valor bindado
    // pode ser int ou enum, dependendo do grupo de chips.
    public class EqualsConverter : IValueConverter
    {
        // Convert/ConvertBack fazem parte da interface IValueConverter (métodos de instância) -
        // não podem virar static sem quebrar a implementação da interface, apesar de não usarem
        // estado desta classe.
#pragma warning disable S2325
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not null && parameter is not null
            && string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
#pragma warning restore S2325
    }
}
