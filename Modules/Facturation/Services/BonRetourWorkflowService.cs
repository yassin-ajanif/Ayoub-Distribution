using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Facturation.Services;

public sealed class BonRetourWorkflowService : IBonRetourWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public BonRetourWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task CreerEtValiderAsync(int bonRetourId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var avoir = await db.BonsRetour
            .Include(a => a.Lignes)
            .FirstAsync(a => a.Id == bonRetourId, cancellationToken);

        if (avoir.Lignes.Count == 0)
            throw new InvalidOperationException("Le bon de retour ne contient aucune ligne.");

        await _stock.SyncBonRetourStockAsync(
            db,
            avoir.Id,
            avoir.Numero,
            avoir.RetourMarchandise,
            avoir.Lignes.Select(l => (l.ProduitId, l.Quantite)),
            userId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }
}
