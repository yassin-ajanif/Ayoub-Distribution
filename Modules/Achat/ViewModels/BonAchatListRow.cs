using System.Globalization;
using BonAchatEntity = GestionCommerciale.Modules.Achat.Models.BonAchat;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Achat.ViewModels;

public sealed class BonAchatListRow
{
    public required BonAchatEntity BonAchat { get; init; }
    public string FournisseurNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string EcheanceShort { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public string HtLabel { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;
    public bool IsOverdue { get; init; }

    public static BonAchatListRow Create(BonAchatEntity f, string fournisseurNom, string devise, ILocaleService locale)
    {
        var (ht, _, ttc) = DocumentTotalsHelper.BonAchatTotals(f.Lignes ?? [], f.RemiseGlobale);
        var isOverdue = !f.EstPayee && f.DateEcheance.Date < DateTime.Today;
        return new BonAchatListRow
        {
            BonAchat = f,
            FournisseurNom = fournisseurNom,
            DateShort = f.Date.ToString("d", CultureInfo.CurrentCulture),
            EcheanceShort = f.DateEcheance.ToString("d", CultureInfo.CurrentCulture),
            StatutLabel = f.EstPayee ? locale.T("Ba_Paid") : locale.T("Ba_Unpaid"),
            HtLabel = locale.Tf("Doc_FmtHt", ht, devise),
            TtcLabel = $"{ttc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(f.Note),
            IsOverdue = isOverdue,
        };
    }
}
