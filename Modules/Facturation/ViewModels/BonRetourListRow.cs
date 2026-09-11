using System.Globalization;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public sealed class BonRetourListRow
{
    public required BonRetour BonRetour { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string MotifDisplay { get; init; } = string.Empty;
    public string HtLabel { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;
    public bool IsTransferred { get; init; }
    public string StatutLabel { get; init; } = string.Empty;
    public string ChipLabel { get; init; } = string.Empty;

    public static BonRetourListRow Create(BonRetour bonRetour, string clientNom, string? linkedBrfNumero, string devise, ILocaleService locale)
    {
        var lines = bonRetour.Lignes ?? [];
        var (ht, _, ttc) = DocumentTotalsHelper.BonRetourTotals(lines);
        var motif = bonRetour.Motif ?? string.Empty;
        const int maxMotif = 72;
        var motifDisplay = motif.Length <= maxMotif ? motif : motif[..maxMotif] + "…";
        var transferred = !string.IsNullOrWhiteSpace(linkedBrfNumero);
        return new BonRetourListRow
        {
            BonRetour = bonRetour,
            ClientNom = clientNom,
            DateShort = bonRetour.Date.ToString("d", CultureInfo.CurrentCulture),
            MotifDisplay = motifDisplay,
            HtLabel = locale.Tf("Doc_FmtHt", ht, devise),
            TtcLabel = $"{ttc:N2} {devise}",
            IsTransferred = transferred,
            StatutLabel = locale.T(transferred ? "Brt_StatutTransferred" : "Brt_StatutNotTransferred"),
            ChipLabel = transferred ? linkedBrfNumero! : locale.T("Brt_StatutNotTransferred"),
        };
    }
}
