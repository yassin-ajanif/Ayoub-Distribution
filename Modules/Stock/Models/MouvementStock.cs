using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Stock.Models;

public class MouvementStock : BaseEntity
{
    public int ProduitId { get; set; }
    public Produit? Produit { get; set; }
    public TypeMouvement Type { get; set; }
    public decimal StockAvant { get; set; }
    public decimal Quantite { get; set; }

    public string OrigineType { get; set; } = string.Empty;
    public int? OrigineId { get; set; }
    public string Note { get; set; } = string.Empty;

    [NotMapped]
    public decimal StockApres => Type switch
    {
        TypeMouvement.Entree => StockAvant + Quantite,
        TypeMouvement.Sortie => StockAvant - Quantite,
        TypeMouvement.Ajustement => StockAvant + Quantite,
        _ => StockAvant
    };

    [NotMapped]
    public decimal SignedQuantite => Type switch
    {
        TypeMouvement.Sortie => -Math.Abs(Quantite),
        TypeMouvement.Entree => Math.Abs(Quantite),
        TypeMouvement.Ajustement => Quantite,
        _ => Quantite
    };

    [NotMapped]
    public string QuantiteSignedLabel
    {
        get
        {
            var signed = SignedQuantite;
            return StockFormatHelper.FormatSigned(signed);
        }
    }

    [NotMapped]
    public string PartyName { get; set; } = string.Empty;

    [NotMapped]
    public bool PartyIsSupplier { get; set; }

    [NotMapped]
    public bool HasPartyName => !string.IsNullOrWhiteSpace(PartyName);

    [NotMapped]
    public decimal PartyColorSignal => PartyIsSupplier ? 1m : -1m;

    [NotMapped]
    public string DocumentRef => string.IsNullOrWhiteSpace(Note) ? OrigineType : Note;

    [NotMapped]
    public bool CanOpenOrigin => OrigineId is > 0 && OrigineType is "BS" or "BA" or "BRT" or "BRF";

    [NotMapped]
    public string TraceDetail => DocumentRef;

    [NotMapped]
    public string UnitPriceDetail { get; set; } = string.Empty;

    [NotMapped]
    public bool HasUnitPriceDetail => !string.IsNullOrEmpty(UnitPriceDetail);
}
