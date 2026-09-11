using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Sortie.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Tiers.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Shared.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tiers> Tiers => Set<Tiers>();
    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();
    public DbSet<BonSortie> BonsSortie => Set<BonSortie>();
    public DbSet<BonSortieLigne> BonSortieLignes => Set<BonSortieLigne>();
    public DbSet<PaiementBonSortie> PaiementsBonSortie => Set<PaiementBonSortie>();
    public DbSet<BonAchat> BonsAchat => Set<BonAchat>();
    public DbSet<BonAchatLigne> BonAchatLignes => Set<BonAchatLigne>();
    public DbSet<PaiementBonAchat> PaiementsBonAchat => Set<PaiementBonAchat>();
    public DbSet<ReglementGroupe> ReglementsGroupes => Set<ReglementGroupe>();
    public DbSet<BonRetour> BonsRetour => Set<BonRetour>();
    public DbSet<BonRetourLigne> BonRetourLignes => Set<BonRetourLigne>();
    public DbSet<BonRetourFournisseur> BonsRetourFournisseurs => Set<BonRetourFournisseur>();
    public DbSet<BonRetourFournisseurLigne> BonRetourFournisseurLignes => Set<BonRetourFournisseurLigne>();
    public DbSet<TypeCharge> TypesCharges => Set<TypeCharge>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<AppSettingsRow> AppSettings => Set<AppSettingsRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tiers>(e =>
        {
            e.ToTable("Tiers", t =>
            {
                t.HasCheckConstraint("CK_Tiers_Categorie", "Categorie IN ('Officiel', 'Comptoir')");
            });
            e.Property(t => t.Type).HasConversion<int>();
            e.Property(t => t.Categorie)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(CategorieTiers.Officiel)
                .IsRequired();
        });

        modelBuilder.Entity<Produit>(e =>
        {
            e.HasOne(p => p.Categorie).WithMany().HasForeignKey(p => p.CategorieId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => p.Reference).IsUnique();
        });

        modelBuilder.Entity<MouvementStock>(e =>
        {
            e.Property(m => m.Type).HasConversion<int>();
            e.HasOne(m => m.Produit).WithMany().HasForeignKey(m => m.ProduitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BonSortie>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.BonSortie).HasForeignKey(l => l.BonSortieId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Paiements).WithOne(p => p.BonSortie).HasForeignKey(p => p.BonSortieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonAchat>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.BonAchat).HasForeignKey(l => l.BonAchatId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Paiements).WithOne(p => p.BonAchat).HasForeignKey(p => p.BonAchatId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaiementBonSortie>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
            e.HasIndex(p => p.ReglementGroupeId);
        });

        modelBuilder.Entity<PaiementBonAchat>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
            e.HasIndex(p => p.ReglementGroupeId);
        });

        modelBuilder.Entity<ReglementGroupe>(e =>
        {
            e.ToTable("ReglementsGroupes");
            e.Property(g => g.Sens).HasConversion<int>();
            e.Property(g => g.Mode).HasConversion<int>();
            e.Property(g => g.Reference).IsRequired();
            e.Property(g => g.Note).IsRequired();
            e.HasIndex(g => g.TiersId);
            e.HasIndex(g => new { g.TiersId, g.Sens });
        });

        modelBuilder.Entity<BonRetour>(e =>
        {
            e.Property(a => a.FactureId);
            e.HasMany(a => a.Lignes).WithOne(l => l.BonRetour).HasForeignKey(l => l.BonRetourId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonRetourFournisseur>(e =>
        {
            e.HasOne(a => a.BonRetour).WithMany().HasForeignKey(a => a.BonRetourId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.BonRetourId);
            e.HasMany(a => a.Lignes).WithOne(l => l.BonRetourFournisseur).HasForeignKey(l => l.BonRetourFournisseurId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TypeCharge>(e =>
        {
            e.Property(t => t.Nom).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.Nom).IsUnique();
        });

        modelBuilder.Entity<Charge>(e =>
        {
            e.Property(c => c.Libelle).HasMaxLength(256).IsRequired();
            e.HasOne(c => c.TypeCharge).WithMany().HasForeignKey(c => c.TypeChargeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => c.TypeChargeId);
            e.HasIndex(c => c.Date);
        });

        modelBuilder.Entity<AppSettingsRow>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var utc = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<GestionCommerciale.Shared.Models.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utc;
                entry.Entity.UpdatedAt = utc;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utc;
            }
        }
    }
}
