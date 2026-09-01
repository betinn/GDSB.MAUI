using System.Globalization;

namespace GDSB.MAUI.Converters
{
    // Usado onde um bloco continua visível mas precisa parecer "desligado" enquanto uma condição
    // não é satisfeita - ex.: o campo de senha antes de escolher o arquivo do cofre (ver
    // UnlockPage.xaml). Diferente de IsVisible, o layout não pula de posição.
    public class BoolToDimOpacityConverter : IValueConverter
    {
        public double EnabledOpacity { get; set; } = 1.0;

        public double DisabledOpacity { get; set; } = 0.45;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b && b ? EnabledOpacity : DisabledOpacity;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
