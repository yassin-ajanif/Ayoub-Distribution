using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.BonRetourFournisseur.Models;

public class BonRetourFournisseur : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int? BonRetourId { get; set; }
    public BonRetour? BonRetour { get; set; }
    public int FournisseurId { get; set; }
    public DateTime Date { get; set; }
    public string Motif { get; set; } = string.Empty;
    public bool RetourMarchandise { get; set; }
    public List<BonRetourFournisseurLigne> Lignes { get; set; } = [];
}
