using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Sortie.Models;

namespace GestionCommerciale.Shared.Helpers;

public static class DocumentTotalsHelper
{
    public const decimal ZeroTotalTolerance = 0.005m;
    public const decimal PaiementTtcTolerance = 0.02m;

    public static bool IsEffectivelyZeroTotal(decimal amount) =>
        Math.Abs(amount) <= ZeroTotalTolerance;

    public static bool PaymentsExceedTtc(decimal ttc, decimal totalPayments) =>
        totalPayments > ttc + PaiementTtcTolerance;

    public static void EnsurePaymentsNotOverTtc(decimal ttc, decimal totalPayments)
    {
        if (PaymentsExceedTtc(ttc, totalPayments))
        {
            throw new InvalidOperationException(
                $"La somme des paiements ({totalPayments:N2} TTC) ne peut pas dépasser le total de la facture ({ttc:N2} TTC).");
        }
    }

    public static decimal LigneHT(decimal qte, decimal puHt, decimal remisePct) =>
        qte * puHt * (1 - remisePct / 100m);

    public static (decimal ht, decimal tva, decimal ttc) BonSortieTotals(IEnumerable<BonSortieLigne> lignes, decimal remiseGlobalePct)
    {
        decimal ht = 0, tva = 0;
        foreach (var l in lignes)
        {
            var lht = LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            ht += lht;
            tva += lht * (l.TauxTVA / 100m);
        }

        if (remiseGlobalePct > 0)
        {
            var factor = 1 - remiseGlobalePct / 100m;
            ht *= factor;
            tva *= factor;
        }

        return (ht, tva, ht + tva);
    }

    public static decimal BonSortieTtc(IEnumerable<BonSortieLigne> lignes, decimal remiseGlobalePct) =>
        BonSortieTotals(lignes, remiseGlobalePct).ttc;

    public static void SyncBonSortieTotalTtc(BonSortie doc) =>
        doc.TotalTtc = BonSortieTtc(doc.Lignes, doc.RemiseGlobale);

    public static (decimal ht, decimal tva, decimal ttc) BonAchatTotals(IEnumerable<BonAchatLigne> lignes, decimal remiseGlobalePct)
    {
        decimal ht = 0, tva = 0;
        foreach (var l in lignes)
        {
            var lht = LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            ht += lht;
            tva += lht * (l.TauxTVA / 100m);
        }

        if (remiseGlobalePct > 0)
        {
            var factor = 1 - remiseGlobalePct / 100m;
            ht *= factor;
            tva *= factor;
        }

        return (ht, tva, ht + tva);
    }

    public static decimal BonAchatTtc(IEnumerable<BonAchatLigne> lignes, decimal remiseGlobalePct) =>
        BonAchatTotals(lignes, remiseGlobalePct).ttc;

    public static void SyncBonAchatTotalTtc(BonAchat doc) =>
        doc.TotalTtc = BonAchatTtc(doc.Lignes, doc.RemiseGlobale);

    public static (decimal ht, decimal tva, decimal ttc) BonRetourTotals(IEnumerable<BonRetourLigne> lignes)
    {
        decimal ht = 0, tva = 0;
        foreach (var l in lignes)
        {
            var lht = LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            ht += lht;
            tva += lht * (l.TauxTVA / 100m);
        }

        return (ht, tva, ht + tva);
    }

    public static (decimal ht, decimal tva, decimal ttc) BonRetourFournisseurTotals(IEnumerable<BonRetourFournisseurLigne> lignes)
    {
        decimal ht = 0, tva = 0;
        foreach (var l in lignes)
        {
            var lht = LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            ht += lht;
            tva += lht * (l.TauxTVA / 100m);
        }

        return (ht, tva, ht + tva);
    }
}
