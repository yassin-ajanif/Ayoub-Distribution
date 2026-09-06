using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GestionCommerciale.Shared.Database;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260906203000_RenameAvoirFournisseurToBonRetourFournisseur")]
    public class RenameAvoirFournisseurToBonRetourFournisseur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL is more reliable on SQLite than RenameIndex helpers.
            migrationBuilder.Sql(
                """
                ALTER TABLE "AvoirsFournisseurs" RENAME TO "BonsRetourFournisseurs";
                ALTER TABLE "AvoirFournisseurLignes" RENAME TO "BonRetourFournisseurLignes";
                ALTER TABLE "BonRetourFournisseurLignes" RENAME COLUMN "AvoirFournisseurId" TO "BonRetourFournisseurId";
                DROP INDEX IF EXISTS "IX_AvoirFournisseurLignes_AvoirFournisseurId";
                CREATE INDEX IF NOT EXISTS "IX_BonRetourFournisseurLignes_BonRetourFournisseurId" ON "BonRetourFournisseurLignes" ("BonRetourFournisseurId");

                UPDATE "BonsRetourFournisseurs"
                SET "Numero" = 'BRF' || SUBSTR("Numero", 4)
                WHERE "Numero" LIKE 'AVF-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'BRF'
                WHERE "OrigineType" = 'AvoirFournisseur';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"AVF"', '"BRF"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"AVF"%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "BonsRetourFournisseurs"
                SET "Numero" = 'AVF' || SUBSTR("Numero", 4)
                WHERE "Numero" LIKE 'BRF-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'AvoirFournisseur'
                WHERE "OrigineType" = 'BRF';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"BRF"', '"AVF"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"BRF"%';

                DROP INDEX IF EXISTS "IX_BonRetourFournisseurLignes_BonRetourFournisseurId";
                ALTER TABLE "BonRetourFournisseurLignes" RENAME COLUMN "BonRetourFournisseurId" TO "AvoirFournisseurId";
                ALTER TABLE "BonRetourFournisseurLignes" RENAME TO "AvoirFournisseurLignes";
                ALTER TABLE "BonsRetourFournisseurs" RENAME TO "AvoirsFournisseurs";
                CREATE INDEX IF NOT EXISTS "IX_AvoirFournisseurLignes_AvoirFournisseurId" ON "AvoirFournisseurLignes" ("AvoirFournisseurId");
                """);
        }
    }
}
