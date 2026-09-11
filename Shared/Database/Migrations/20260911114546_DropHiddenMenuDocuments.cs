using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    public partial class DropHiddenMenuDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                PRAGMA foreign_keys = OFF;
                DROP TABLE IF EXISTS "Paiements";
                DROP TABLE IF EXISTS "FactureLignes";
                DROP TABLE IF EXISTS "Factures";
                DROP TABLE IF EXISTS "PaiementsFournisseurs";
                DROP TABLE IF EXISTS "FactureFournisseurLignes";
                DROP TABLE IF EXISTS "FacturesFournisseurs";
                DROP TABLE IF EXISTS "BonLivraisonLignes";
                DROP TABLE IF EXISTS "BonsLivraison";
                DROP TABLE IF EXISTS "BonCommandeClientLignes";
                DROP TABLE IF EXISTS "BonsCommandeClient";
                DROP TABLE IF EXISTS "BonReceptionLignes";
                DROP TABLE IF EXISTS "BonsReception";
                DROP TABLE IF EXISTS "BonCommandeLignes";
                DROP TABLE IF EXISTS "BonsCommande";
                DROP TABLE IF EXISTS "DevisLignes";
                DROP TABLE IF EXISTS "Devis";
                DROP INDEX IF EXISTS "IX_BonsRetour_FactureId";
                CREATE TABLE "BonsRetour_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_BonsRetour" PRIMARY KEY AUTOINCREMENT,
                    "ClientId" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "CreatedByUserId" INTEGER NULL,
                    "Date" TEXT NOT NULL,
                    "FactureId" INTEGER NULL,
                    "Motif" TEXT NOT NULL,
                    "Numero" TEXT NOT NULL,
                    "RetourMarchandise" INTEGER NOT NULL,
                    "UpdatedAt" TEXT NOT NULL
                );
                INSERT INTO "BonsRetour_new" ("Id", "ClientId", "CreatedAt", "CreatedByUserId", "Date", "FactureId", "Motif", "Numero", "RetourMarchandise", "UpdatedAt")
                SELECT "Id", "ClientId", "CreatedAt", "CreatedByUserId", "Date", "FactureId", "Motif", "Numero", "RetourMarchandise", "UpdatedAt"
                FROM "BonsRetour";
                DROP TABLE "BonsRetour";
                ALTER TABLE "BonsRetour_new" RENAME TO "BonsRetour";
                PRAGMA foreign_keys = ON;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hidden documents are not restored. Recreating those tables would also
            // require SQLite table rebuilds that this provider does not support.
        }
    }
}
