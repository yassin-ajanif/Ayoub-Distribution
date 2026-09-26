using System.Globalization;
using Avalonia.Data.Converters;

namespace GestionCommerciale.Shared.Helpers
{
    public static class StockFormatHelper
    {
        public static string Format(decimal value, IFormatProvider? provider = null)
        {
            provider ??= CultureInfo.CurrentCulture;
            if (value == decimal.Truncate(value))
                return value.ToString("N0", provider);
            return value.ToString("0.##", provider);
        }

        public static string FormatSigned(decimal value, IFormatProvider? provider = null)
        {
            var abs = Format(Math.Abs(value), provider);
            return value >= 0 ? $"+{abs}" : $"-{abs}";
        }
    }

    public sealed class StockQuantityConverter : IValueConverter
    {
        public static readonly StockQuantityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                decimal d => StockFormatHelper.Format(d, culture),
                double dbl => StockFormatHelper.Format((decimal)dbl, culture),
                float f => StockFormatHelper.Format((decimal)f, culture),
                int i => StockFormatHelper.Format(i, culture),
                long l => StockFormatHelper.Format(l, culture),
                _ => value?.ToString()
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
