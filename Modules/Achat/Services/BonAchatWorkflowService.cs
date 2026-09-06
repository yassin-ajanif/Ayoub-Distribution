using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Achat.Services;

public sealed class BonAchatWorkflowService : IBonAchatWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BonAchatWorkflowService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task AddPaiementAsync(int factureId, PaiementBonAchat paiement, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsAchat
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonAchatTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Sum(p => p.Montant) + paiement.Montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        paiement.BonAchatId = factureId;
        db.PaiementsBonAchat.Add(paiement);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default)
    {
        if (montant <= 0)
            throw new InvalidOperationException("Le montant doit être supérieur à 0.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsAchat
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonAchatTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Where(x => x.Id != paiementId).Sum(x => x.Montant) + montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        var p = await db.PaiementsBonAchat.FirstAsync(x => x.Id == paiementId && x.BonAchatId == factureId, cancellationToken);
        p.Montant = montant;
        p.Date = date;
        p.Mode = mode;
        p.Reference = reference;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var p = await db.PaiementsBonAchat.FirstAsync(x => x.Id == paiementId && x.BonAchatId == factureId, cancellationToken);
        db.PaiementsBonAchat.Remove(p);
        await db.SaveChangesAsync(cancellationToken);
    }
}
