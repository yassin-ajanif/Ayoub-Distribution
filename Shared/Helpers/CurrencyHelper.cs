using System.Globalization;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Shared.Helpers;

public static class CurrencyHelper
{
    public const string DefaultCode = "DH";

    public static string Format(decimal amount, string currencyCode = DefaultCode)
    {
        var c = CultureInfo.GetCultureInfo("fr-FR");
        var code = string.IsNullOrWhiteSpace(currencyCode) ? DefaultCode : currencyCode.Trim();
        return amount.ToString("N2", c) + " " + code;
    }

    public static string FromSettings(AppSettingsRow cfg) =>
        string.IsNullOrWhiteSpace(cfg.Devise) ? DefaultCode : cfg.Devise.Trim();
}
