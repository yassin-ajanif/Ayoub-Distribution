using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Sortie.ViewModels;

public partial class BonSortieLineRow : ObservableObject
{
    public const string PromoSuffix = " (promo)";
    public const string PromoLabel = "(promo)";

    [ObservableProperty] private int _produitId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private string _conditionnement = string.Empty;
    [ObservableProperty] private decimal _quantite = 1;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;
    [ObservableProperty] private bool _isPromo;
    [ObservableProperty] private decimal _prixCatalogueHt;

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);

    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    public decimal MontantCatalogueHt => DocumentTotalsHelper.LigneHT(Quantite, PrixCatalogueHt, 0);
    public decimal MontantCatalogueTtc => MontantCatalogueHt * (1 + TauxTva / 100m);

    public string PromoPuLabel => PrixCatalogueHt.ToString("0.##");
    public string PromoMontantHtLabel => MontantCatalogueHt.ToString("N2");
    public string PromoMontantTtcLabel => MontantCatalogueTtc.ToString("N2");

    /// <summary>Designation stored on the document (includes promo marker when needed).</summary>
    public string DesignationForPersist
    {
        get
        {
            var baseName = Designation.Trim();
            if (!IsPromo)
                return baseName;
            return LooksLikePromo(baseName) ? baseName : baseName + PromoSuffix;
        }
    }

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();
    partial void OnPrixCatalogueHtChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogProduct(Produit p)
    {
        IsPromo = false;
        ProduitId = p.Id;
        Reference = p.Reference;
        Designation = p.Designation;
        Conditionnement = p.Unite;
        PrixUnitaireHt = p.PrixVenteHT;
        TauxTva = p.TauxTVA;
        NotifyMontants();
    }

    public void ApplyPromoCatalogProduct(Produit p)
    {
        ApplyCatalogProduct(p);
        IsPromo = true;
        Designation = StripPromoSuffix(p.Designation);
        PrixCatalogueHt = p.PrixVenteHT;
        PrixUnitaireHt = 0;
        Remise = 0;
        NotifyMontants();
    }

    public static bool LooksLikePromo(string designation) =>
        designation.Contains(PromoSuffix, StringComparison.OrdinalIgnoreCase)
        || designation.TrimEnd().EndsWith(PromoLabel, StringComparison.OrdinalIgnoreCase);

    public static string StripPromoSuffix(string designation)
    {
        var s = designation.Trim();
        if (s.EndsWith(PromoSuffix, StringComparison.OrdinalIgnoreCase))
            return s[..^PromoSuffix.Length].TrimEnd();
        if (s.EndsWith(PromoLabel, StringComparison.OrdinalIgnoreCase))
            return s[..^PromoLabel.Length].TrimEnd();
        return s;
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
        OnPropertyChanged(nameof(MontantCatalogueHt));
        OnPropertyChanged(nameof(MontantCatalogueTtc));
        OnPropertyChanged(nameof(PromoPuLabel));
        OnPropertyChanged(nameof(PromoMontantHtLabel));
        OnPropertyChanged(nameof(PromoMontantTtcLabel));
    }
}
