using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Sortie.Models;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Models.Pdf;

namespace GestionCommerciale.Shared.Services;

public interface IPdfService
{
    Task<byte[]> BuildBonRetourFournisseurPdfAsync(BonRetourFournisseur doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonSortiePdfAsync(BonSortie doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonAchatPdfAsync(BonAchat doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonRetourPdfAsync(BonRetour bonRetour, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildClientAccountStatementPdfAsync(
        Tiers client,
        ClientAccountStatementResult statement,
        DocumentPartyPdfInfo party,
        CancellationToken cancellationToken = default);
    Task<byte[]> BuildSupplierAccountStatementPdfAsync(
        Tiers fournisseur,
        ClientAccountStatementResult statement,
        DocumentPartyPdfInfo party,
        CancellationToken cancellationToken = default);
}
