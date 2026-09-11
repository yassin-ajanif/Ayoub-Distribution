using GestionCommerciale.Modules.Facturation.Services;

namespace GestionCommerciale.Modules.FactureFournisseur.Services;

public interface ISupplierAccountStatementService
{
    Task<ClientAccountStatementResult> GetStatementAsync(int fournisseurId, CancellationToken cancellationToken = default);
}
