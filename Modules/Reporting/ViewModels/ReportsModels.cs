using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Reporting.ViewModels;

public sealed class ReportSaleByProductRow
{
    public ReportSaleByProductRow(string reference, string designation, string categorie,
        decimal quantite, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Reference = reference;
        Designation = designation;
        Categorie = categorie;
        Quantite = quantite;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblQty = quantite.ToString("N2");
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Reference { get; }
    public string Designation { get; }
    public string Categorie { get; }
    public decimal Quantite { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblQty { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed class ReportSaleByCustomerDocLineRow
{
    public ReportSaleByCustomerDocLineRow(
        string designation,
        decimal quantite,
        decimal totalTtc,
        decimal promoTtc,
        decimal earned,
        string devise,
        bool isBonSortie,
        bool isPromo)
    {
        Designation = designation;
        IsPromo = isPromo;
        IsReturn = !isBonSortie;
        IsSale = isBonSortie && !isPromo;
        LblQty = quantite.ToString("N2");
        LblTtc = $"{(isBonSortie ? "" : "-")}{totalTtc:N2} {devise}";
        LblPromo = isPromo ? $"-{promoTtc:N2} {devise}" : "—";
        var earnedSign = earned > 0 ? "+" : "";
        LblEarned = $"{earnedSign}{earned:N2} {devise}";
        IsEarnedPositive = earned >= 0;
    }

    public string Designation { get; }
    public bool IsPromo { get; }
    public bool IsReturn { get; }
    public bool IsSale { get; }
    public string LblQty { get; }
    public string LblTtc { get; }
    public string LblPromo { get; }
    public string LblEarned { get; }
    public bool IsEarnedPositive { get; }
}

public sealed partial class ReportSaleByCustomerDocRow : ObservableObject
{
    public ReportSaleByCustomerDocRow(
        bool isBonSortie,
        int documentId,
        string typeLabel,
        string numero,
        DateTime date,
        decimal totalTtc,
        decimal promoTtc,
        decimal earned,
        string devise,
        List<ReportSaleByCustomerDocLineRow>? lines = null)
    {
        IsBonSortie = isBonSortie;
        DocumentId = documentId;
        TypeLabel = typeLabel;
        Numero = numero;
        Date = date;
        TotalTtc = totalTtc;
        PromoTtc = promoTtc;
        Earned = earned;
        LblDate = date.ToString("d");
        LblTtc = $"{(isBonSortie ? "" : "-")}{totalTtc:N2} {devise}";
        LblPromo = promoTtc > 0 ? $"-{promoTtc:N2} {devise}" : "—";
        var earnedSign = earned > 0 ? "+" : "";
        LblEarned = $"{earnedSign}{earned:N2} {devise}";
        IsEarnedPositive = earned >= 0;
        if (lines != null)
        {
            foreach (var line in lines)
                _lines.Add(line);
        }
    }

    public bool IsBonSortie { get; }
    public bool IsReturn => !IsBonSortie;
    public int DocumentId { get; }
    public string TypeLabel { get; }
    public string Numero { get; }
    public DateTime Date { get; }
    public decimal TotalTtc { get; }
    public decimal PromoTtc { get; }
    public bool HasPromo => PromoTtc > 0;
    public decimal Earned { get; }
    public string LblDate { get; }
    public string LblTtc { get; }
    public string LblPromo { get; }
    public string LblEarned { get; }
    public bool IsEarnedPositive { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportSaleByCustomerDocLineRow> _lines = [];
    public ObservableCollection<ReportSaleByCustomerDocLineRow> Lines => _lines;
}

public sealed partial class ReportSaleByCustomerRow : ObservableObject
{
    public ReportSaleByCustomerRow(string client, string ice,
        int nbBonsSortie, int nbBonsRetour,
        decimal totalTtc, decimal totalPromo, decimal totalReturns, decimal earned, string devise,
        List<ReportSaleByCustomerDocRow>? documents = null)
    {
        Client = client;
        Ice = ice;
        NbBonsSortie = nbBonsSortie;
        NbBonsRetour = nbBonsRetour;
        TotalTtc = totalTtc;
        TotalPromo = totalPromo;
        TotalReturns = totalReturns;
        Profit = earned;
        Devise = devise;
        var parts = new List<string>();
        if (nbBonsSortie > 0)
            parts.Add($"{nbBonsSortie} BS");
        if (nbBonsRetour > 0)
            parts.Add($"{nbBonsRetour} BR");
        LblCount = parts.Count == 0 ? "—" : string.Join(" · ", parts);
        LblTtc = $"{totalTtc:N2} {devise}";
        LblPromo = $"-{totalPromo:N2} {devise}";
        LblReturns = $"-{totalReturns:N2} {devise}";
        var earnedSign = earned > 0 ? "+" : "";
        LblProfit = $"{earnedSign}{earned:N2} {devise}";
        IsEarnedPositive = earned >= 0;
        if (documents != null)
        {
            foreach (var doc in documents)
                _documents.Add(doc);
        }
    }

    public string Client { get; }
    public string Ice { get; }
    public int NbBonsSortie { get; }
    public int NbBonsRetour { get; }
    public decimal TotalTtc { get; }
    public decimal TotalPromo { get; }
    public decimal TotalReturns { get; }
    public decimal Profit { get; }
    public string Devise { get; }
    public string LblCount { get; }
    public string LblTtc { get; }
    public string LblPromo { get; }
    public string LblReturns { get; }
    public string LblProfit { get; }
    public bool IsEarnedPositive { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportSaleByCustomerDocRow> _documents = [];
    public ObservableCollection<ReportSaleByCustomerDocRow> Documents => _documents;
}

public sealed class ReportDailySaleDetailRow
{
    public ReportDailySaleDetailRow(string numero, string client,
        decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Numero = numero;
        Client = client;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Numero { get; }
    public string Client { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed partial class ReportDailySaleRow : ObservableObject
{
    public ReportDailySaleRow(DateTime date, int nbFactures,
        decimal totalHt, decimal totalTva, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        List<ReportDailySaleDetailRow>? details = null)
    {
        Date = date;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTva = totalTva;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblDate = date.ToString("d");
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTva = $"{totalTva:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (details != null)
        {
            foreach (var d in details)
                _details.Add(d);
        }
    }

    public DateTime Date { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTva { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTva { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportDailySaleDetailRow> _details = [];
    public ObservableCollection<ReportDailySaleDetailRow> Details => _details;
}

public sealed class ReportStockMovementRow
{
    public ReportStockMovementRow(DateTime date, string produitRef, string produitDesignation,
        string typeMvt, decimal quantite, string origine, decimal stockApres)
    {
        Date = date;
        ProduitRef = produitRef;
        ProduitDesignation = produitDesignation;
        TypeMvt = typeMvt;
        Quantite = quantite;
        Origine = origine;
        StockApres = stockApres;
        LblDate = date.ToString("g");
        LblQty = quantite.ToString("N2");
        LblStockApres = stockApres.ToString("N2");
    }

    public DateTime Date { get; }
    public string ProduitRef { get; }
    public string ProduitDesignation { get; }
    public string TypeMvt { get; }
    public decimal Quantite { get; }
    public string Origine { get; }
    public decimal StockApres { get; }
    public string LblDate { get; }
    public string LblQty { get; }
    public string LblStockApres { get; }
}

public enum ReportProfitChargeKind
{
    SaleMargin,
    BonRetourClient,
    Purchase,
    BonRetourFournisseur,
    Charge,
    Promo
}

public sealed class ReportProfitChargeRow
{
    public ReportProfitChargeRow(
        ReportProfitChargeKind kind,
        string typeLabel,
        string refLibelle,
        DateTime date,
        decimal montantHt,
        decimal amount,
        string devise,
        bool isPositive,
        int documentId)
    {
        Kind = kind;
        TypeLabel = typeLabel;
        RefLibelle = refLibelle;
        Date = date;
        MontantHt = montantHt;
        Amount = amount;
        Devise = devise;
        IsPositive = isPositive;
        DocumentId = documentId;
        LblDate = date.ToString("d");
        LblMontantHt = montantHt > 0 ? $"{montantHt:N2} {devise}" : "—";
        var sign = amount >= 0 ? "+" : "";
        LblAmount = $"{sign}{amount:N2} {devise}";
    }

    public ReportProfitChargeKind Kind { get; }
    public int DocumentId { get; }
    public string TypeLabel { get; }
    public string RefLibelle { get; }
    public DateTime Date { get; }
    public decimal MontantHt { get; }
    public decimal Amount { get; }
    public string Devise { get; }
    public bool IsPositive { get; }
    public string LblDate { get; }
    public string LblMontantHt { get; }
    public string LblAmount { get; }
}

public sealed class ReportProfitChargesResult
{
    public required decimal TotalSalesMargin { get; init; }
    public required decimal TotalVente { get; init; }
    public required decimal TotalBonsRetourClient { get; init; }
    public required decimal TotalPurchases { get; init; }
    public required decimal TotalBonsRetourFournisseur { get; init; }
    public required decimal TotalCharges { get; init; }
    public required decimal TotalPromo { get; init; }
    public required decimal NetResult { get; init; }
    public required string Devise { get; init; }
    public required List<ReportProfitChargeRow> Rows { get; init; }
}
