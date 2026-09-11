using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class BonRetour : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    /// <summary>Legacy column kept in SQLite. No longer linked to a client invoice.</summary>
    public int? FactureId { get; set; }
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public string Motif { get; set; } = string.Empty;
    public bool RetourMarchandise { get; set; }
    public List<BonRetourLigne> Lignes { get; set; } = [];
}
