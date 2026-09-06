using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class BonRetourLigne : BaseEntity
{
    public int BonRetourId { get; set; }
    public BonRetour? BonRetour { get; set; }
    public int ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    public string Conditionnement { get; set; } = string.Empty;
}
