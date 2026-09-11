using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Auth.ViewModels;

internal static class DocumentNumberingQuery
{
    public static Task<List<string>> LoadNumerosAsync(AppDbContext db, string prefix, CancellationToken cancellationToken) =>
        prefix.ToUpperInvariant() switch
        {
            "BS" => db.BonsSortie.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken),
            "BA" => db.BonsAchat.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken),
            "BRT" => db.BonsRetour.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken),
            "BRF" => db.BonsRetourFournisseurs.AsNoTracking().Select(d => d.Numero).ToListAsync(cancellationToken),
            _ => Task.FromResult(new List<string>())
        };
}
