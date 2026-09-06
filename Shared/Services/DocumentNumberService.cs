using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Shared.Services;

public sealed class DocumentNumberService : IDocumentNumberService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppSettingsService _settings;

    public DocumentNumberService(IDbContextFactory<AppDbContext> dbFactory, IAppSettingsService settings)
    {
        _dbFactory = dbFactory;
        _settings = settings;
    }

    public Task<string> NextDevisAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.Devis.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "DEV", cancellationToken);

    public Task<string> NextBLAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsLivraison.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BL", cancellationToken);

    public Task<string> NextBRAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsReception.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BR", cancellationToken);

    public Task<string> NextBCAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsCommande.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BC", cancellationToken);

    public Task<string> NextBCClientAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsCommandeClient.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BCC", cancellationToken);

    public Task<string> NextFactureAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.Factures.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "FAC", cancellationToken);

    public Task<string> NextBonSortieAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsSortie.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BS", cancellationToken);

    public Task<string> NextBonAchatAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsAchat.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BA", cancellationToken);

    public Task<string> NextFactureFournisseurAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.FacturesFournisseurs.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "FAF", cancellationToken);

    public Task<string> NextBonRetourAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsRetour.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BRT", cancellationToken);

    public Task<string> NextBonRetourFournisseurAsync(CancellationToken cancellationToken = default) =>
        NextFromDbAsync(db => db.BonsRetourFournisseurs.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken), "BRF", cancellationToken);

    private async Task<string> NextFromDbAsync(
        Func<AppDbContext, Task<List<string>>> loadNumeros,
        string prefix,
        CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var numeros = await loadNumeros(db);
        var dbMax = DocumentNumberingHelper.GetMaxSequenceFromNumeros(numeros, prefix, year);

        var settings = await _settings.GetAsync(cancellationToken);
        var floors = DocumentNumberingFloorsStorage.Parse(settings.DocumentNumberingFloorsJson);
        var lastUsedOutside = DocumentNumberingFloorsStorage.GetLastUsedOutside(floors, prefix, year);

        var next = DocumentNumberingHelper.ResolveNextSequence(dbMax, lastUsedOutside);
        return NumberingHelper.Generate(prefix, next - 1, year);
    }
}
