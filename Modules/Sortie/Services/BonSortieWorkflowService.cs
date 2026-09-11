using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Sortie.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Sortie.Services;

public sealed class BonSortieWorkflowService : IBonSortieWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BonSortieWorkflowService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task AddPaiementAsync(int factureId, PaiementBonSortie paiement, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsSortie
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonSortieTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Sum(p => p.Montant) + paiement.Montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        paiement.BonSortieId = factureId;
        db.PaiementsBonSortie.Add(paiement);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default)
    {
        if (montant <= 0)
            throw new InvalidOperationException("Le montant doit être supérieur à 0.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsSortie
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonSortieTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Where(x => x.Id != paiementId).Sum(x => x.Montant) + montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        var p = await db.PaiementsBonSortie.FirstAsync(x => x.Id == paiementId && x.BonSortieId == factureId, cancellationToken);
        p.Montant = montant;
        p.Date = date;
        p.Mode = mode;
        p.Reference = reference;
        await ReglementGroupeSync.RecalculateAsync(db, p.ReglementGroupeId, paiementId, null, montant, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var p = await db.PaiementsBonSortie.FirstAsync(x => x.Id == paiementId && x.BonSortieId == factureId, cancellationToken);
        var groupeId = p.ReglementGroupeId;
        db.PaiementsBonSortie.Remove(p);
        await ReglementGroupeSync.RecalculateAsync(db, groupeId, paiementId, null, 0, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
