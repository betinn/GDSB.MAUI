using System.Globalization;

namespace GDSB.MAUI.Converters
{
    public class InvertedBoolConverter : IValueConverter
    {
        // Convert/ConvertBack fazem parte da interface IValueConverter (métodos de instância) -
        // não podem virar static sem quebrar a implementação da interface, apesar de não usarem
        // estado desta classe.
#pragma warning disable S2325
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b && !b;
#pragma warning restore S2325
    }
}
