using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Sortie.Models;

namespace GestionCommerciale.Modules.Sortie.Services;

public interface IBonSortieWorkflowService
{
    Task AddPaiementAsync(int factureId, PaiementBonSortie paiement, CancellationToken cancellationToken = default);
    Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default);
    Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default);
}
