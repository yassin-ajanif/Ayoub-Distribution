using System.Globalization;
using Avalonia.Data.Converters;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Shared.Converters;

public sealed class CategorieTiersLabelConverter : IValueConverter
{
    public static readonly CategorieTiersLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CategorieTiers c)
            return value?.ToString() ?? string.Empty;
        var lang = culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "fr";
        var key = c == CategorieTiers.Comptoir ? "CategorieTiers_Comptoir" : "CategorieTiers_Officiel";
        return UiTranslations.Get(key, lang);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
