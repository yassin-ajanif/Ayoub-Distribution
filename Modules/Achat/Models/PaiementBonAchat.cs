using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Achat.Models;

public class PaiementBonAchat : BaseEntity
{
    public int BonAchatId { get; set; }
    public BonAchat? BonAchat { get; set; }
    /// <summary>Set when this row is a slice of a handed amount stored on <see cref="Facturation.Models.ReglementGroupe"/>.</summary>
    public int? ReglementGroupeId { get; set; }
    public decimal Montant { get; set; }
    public DateTime Date { get; set; }
    public ModePaiement Mode { get; set; }
    public string Reference { get; set; } = string.Empty;
}
