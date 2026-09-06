namespace GestionCommerciale.Shared.Database;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@local";
    public const string DefaultAdminPassword = "admin";
    public const string DefaultClientName = "Vendeur Comptoir";

    public static void Seed(AppDbContext db)
    {
        if (!db.AppSettings.Any())
        {
            db.AppSettings.Add(new AppSettingsRow { Id = 1 });
            db.SaveChanges();
        }

        foreach (var legacyName in new[] { "Client Comptoire", "Client Comptoir" })
        {
            var legacy = db.Tiers.FirstOrDefault(t => t.Nom == legacyName);
            if (legacy != null && !db.Tiers.Any(t => t.Nom == DefaultClientName))
            {
                legacy.Nom = DefaultClientName;
                db.SaveChanges();
            }
        }

        if (!db.Tiers.Any(t => t.Nom == DefaultClientName))
        {
            db.Tiers.Add(new GestionCommerciale.Modules.Tiers.Models.Tiers
            {
                Nom = DefaultClientName,
                Type = GestionCommerciale.Modules.Tiers.Models.TypeTiers.Client,
                Categorie = GestionCommerciale.Modules.Tiers.Models.CategorieTiers.Comptoir,
                Actif = true
            });
            db.SaveChanges();
        }
    }
}
