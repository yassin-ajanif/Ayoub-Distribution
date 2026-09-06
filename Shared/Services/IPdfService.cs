using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.CommandeFournisseur.Models;
using GestionCommerciale.Modules.CommandeClient.Models;
using GestionCommerciale.Modules.Devis.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.Sortie.Models;
using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Models.Pdf;

namespace GestionCommerciale.Shared.Services;

public interface IPdfService
{
    Task<byte[]> BuildBonRetourFournisseurPdfAsync(BonRetourFournisseur doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildDevisPdfAsync(Devis devis, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonLivraisonPdfAsync(BonLivraison bl, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonReceptionPdfAsync(BonReception br, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonCommandePdfAsync(BonCommande bc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonCommandeClientPdfAsync(BonCommandeClient bc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildFacturePdfAsync(Facture facture, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonSortiePdfAsync(BonSortie doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildBonAchatPdfAsync(BonAchat doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
    Task<byte[]> BuildFactureFournisseurPdfAsync(FactureFournisseur factureFournisseur, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default);
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
