namespace GestionCommerciale.Shared.Services;

public interface IDocumentNumberService
{
    Task<string> NextBonSortieAsync(CancellationToken cancellationToken = default);
    Task<string> NextBonAchatAsync(CancellationToken cancellationToken = default);
    Task<string> NextBonRetourAsync(CancellationToken cancellationToken = default);
    Task<string> NextBonRetourFournisseurAsync(CancellationToken cancellationToken = default);
}
