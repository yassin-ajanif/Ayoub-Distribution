using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GestionCommerciale.Shared.Database;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260906195000_RenameAvoirToBonRetour")]
    public class RenameAvoirToBonRetour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL is more reliable on SQLite than RenameIndex helpers.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Avoirs" RENAME TO "BonsRetour";
                ALTER TABLE "AvoirLignes" RENAME TO "BonRetourLignes";
                ALTER TABLE "BonRetourLignes" RENAME COLUMN "AvoirId" TO "BonRetourId";
                DROP INDEX IF EXISTS "IX_AvoirLignes_AvoirId";
                CREATE INDEX IF NOT EXISTS "IX_BonRetourLignes_BonRetourId" ON "BonRetourLignes" ("BonRetourId");

                UPDATE "BonsRetour"
                SET "Numero" = 'BRT' || SUBSTR("Numero", 4)
                WHERE "Numero" LIKE 'AVO-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'BRT'
                WHERE "OrigineType" = 'Avoir';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"AVO"', '"BRT"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"AVO"%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "BonsRetour"
                SET "Numero" = 'AVO' || SUBSTR("Numero", 4)
                WHERE "Numero" LIKE 'BRT-%';

                UPDATE "MouvementsStock"
                SET "OrigineType" = 'Avoir'
                WHERE "OrigineType" = 'BRT';

                UPDATE "AppSettings"
                SET "DocumentNumberingFloorsJson" = REPLACE("DocumentNumberingFloorsJson", '"BRT"', '"AVO"')
                WHERE "DocumentNumberingFloorsJson" LIKE '%"BRT"%';

                DROP INDEX IF EXISTS "IX_BonRetourLignes_BonRetourId";
                ALTER TABLE "BonRetourLignes" RENAME COLUMN "BonRetourId" TO "AvoirId";
                ALTER TABLE "BonRetourLignes" RENAME TO "AvoirLignes";
                ALTER TABLE "BonsRetour" RENAME TO "Avoirs";
                CREATE INDEX IF NOT EXISTS "IX_AvoirLignes_AvoirId" ON "AvoirLignes" ("AvoirId");
                """);
        }
    }
}
