namespace GestionCommerciale.Shared.Helpers;

public static class DocumentNumberKind
{
    public sealed record Entry(string Prefix, string LabelKey);

    public static readonly Entry[] All =
    [
        new("BS", "Nav_BonSortie"),
        new("BA", "Nav_BonAchat"),
        new("BRT", "Nav_BonRetour"),
        new("BRF", "Nav_BonRetourFournisseur"),
    ];
}
