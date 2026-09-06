using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Achat.Models;

public class BonAchat : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int FournisseurId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateEcheance { get; set; }
    public bool EstPayee { get; set; }
    public decimal RemiseGlobale { get; set; }
    public decimal TotalTtc { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<BonAchatLigne> Lignes { get; set; } = [];
    public List<PaiementBonAchat> Paiements { get; set; } = [];
}
