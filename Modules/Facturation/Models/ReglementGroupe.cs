using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

/// <summary>One handed amount (for example 150) that is later sliced across documents.</summary>
public class ReglementGroupe : BaseEntity
{
    public int TiersId { get; set; }
    public SensReglement Sens { get; set; }
    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public ModePaiement Mode { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
