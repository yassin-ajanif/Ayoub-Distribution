using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameBonPreparationToBonSortie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BonsPreparation" RENAME TO "BonsSortie";
                ALTER TABLE "BonPreparationLignes" RENAME TO "BonSortieLignes";
                ALTER TABLE "PaiementsBonPreparation" RENAME TO "PaiementsBonSortie";
                ALTER TABLE "BonSortieLignes" RENAME COLUMN "BonPreparationId" TO "BonSortieId";
                ALTER TABLE "PaiementsBonSortie" RENAME COLUMN "BonPreparationId" TO "BonSortieId";
                DROP INDEX IF EXISTS "IX_BonPreparationLignes_BonPreparationId";
                DROP INDEX IF EXISTS "IX_PaiementsBonPreparation_BonPreparationId";
                CREATE INDEX IF NOT EXISTS "IX_BonSortieLignes_BonSortieId" ON "BonSortieLignes" ("BonSortieId");
                CREATE INDEX IF NOT EXISTS "IX_PaiementsBonSortie_BonSortieId" ON "PaiementsBonSortie" ("BonSortieId");

                UPDATE "BonsSortie"
                SET "Numero" = 'BS' || SUBSTR("Numero", 3)
                WHERE "Numero" LIKE 'BP-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'BS'
                WHERE "OrigineType" = 'BP';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"BP"', '"BS"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"BP"%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "BonsSortie"
                SET "Numero" = 'BP' || SUBSTR("Numero", 3)
                WHERE "Numero" LIKE 'BS-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'BP'
                WHERE "OrigineType" = 'BS';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"BS"', '"BP"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"BS"%';

                DROP INDEX IF EXISTS "IX_BonSortieLignes_BonSortieId";
                DROP INDEX IF EXISTS "IX_PaiementsBonSortie_BonSortieId";
                ALTER TABLE "BonSortieLignes" RENAME COLUMN "BonSortieId" TO "BonPreparationId";
                ALTER TABLE "PaiementsBonSortie" RENAME COLUMN "BonSortieId" TO "BonPreparationId";
                ALTER TABLE "PaiementsBonSortie" RENAME TO "PaiementsBonPreparation";
                ALTER TABLE "BonSortieLignes" RENAME TO "BonPreparationLignes";
                ALTER TABLE "BonsSortie" RENAME TO "BonsPreparation";
                CREATE INDEX IF NOT EXISTS "IX_BonPreparationLignes_BonPreparationId" ON "BonPreparationLignes" ("BonPreparationId");
                CREATE INDEX IF NOT EXISTS "IX_PaiementsBonPreparation_BonPreparationId" ON "PaiementsBonPreparation" ("BonPreparationId");
                """);
        }
    }
}
