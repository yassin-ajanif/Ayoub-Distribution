using System.Globalization;
using BonSortieEntity = GestionCommerciale.Modules.Sortie.Models.BonSortie;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Sortie.ViewModels;

public sealed class BonSortieListRow
{
    public required BonSortieEntity BonSortie { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string EcheanceShort { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public string HtLabel { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;
    public bool IsOverdue { get; init; }

    public static BonSortieListRow Create(BonSortieEntity f, string clientNom, string devise, ILocaleService locale)
    {
        var (ht, _, ttc) = DocumentTotalsHelper.BonSortieTotals(f.Lignes ?? [], f.RemiseGlobale);
        var isOverdue = !f.EstPayee && f.DateEcheance.Date < DateTime.Today;
        return new BonSortieListRow
        {
            BonSortie = f,
            ClientNom = clientNom,
            DateShort = f.Date.ToString("d", CultureInfo.CurrentCulture),
            EcheanceShort = f.DateEcheance.ToString("d", CultureInfo.CurrentCulture),
            StatutLabel = f.EstPayee ? locale.T("Bs_Paid") : locale.T("Bs_Unpaid"),
            HtLabel = locale.Tf("Doc_FmtHt", ht, devise),
            TtcLabel = $"{ttc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(f.Note),
            IsOverdue = isOverdue,
        };
    }
}
