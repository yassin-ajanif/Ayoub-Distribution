using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Sortie.Models;

public class PaiementBonSortie : BaseEntity
{
    public int BonSortieId { get; set; }
    public BonSortie? BonSortie { get; set; }
    public decimal Montant { get; set; }
    public DateTime Date { get; set; }
    public ModePaiement Mode { get; set; }
    public string Reference { get; set; } = string.Empty;
}
