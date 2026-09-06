using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Achat.Models;

namespace GestionCommerciale.Modules.Achat.Services;

public interface IBonAchatWorkflowService
{
    Task AddPaiementAsync(int factureId, PaiementBonAchat paiement, CancellationToken cancellationToken = default);
    Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default);
    Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default);
}
