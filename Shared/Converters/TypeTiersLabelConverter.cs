using System.Globalization;
using Avalonia.Data.Converters;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Shared.Converters;

public sealed class TypeTiersLabelConverter : IValueConverter
{
    public static readonly TypeTiersLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TypeTiers t)
            return value?.ToString() ?? string.Empty;
        var lang = culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "fr";
        var key = t switch
        {
            TypeTiers.Fournisseur => "TypeTiers_Fournisseur",
            TypeTiers.LesDeux => "TypeTiers_LesDeux",
            _ => "TypeTiers_Vendeur",
        };
        return UiTranslations.Get(key, lang);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
