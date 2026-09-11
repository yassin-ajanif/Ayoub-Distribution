using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReglementsGroupes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "ReglementsGroupes" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ReglementsGroupes" PRIMARY KEY AUTOINCREMENT,
                    "TiersId" INTEGER NOT NULL,
                    "Sens" INTEGER NOT NULL,
                    "Date" TEXT NOT NULL,
                    "Montant" TEXT NOT NULL,
                    "Mode" INTEGER NOT NULL,
                    "Reference" TEXT NOT NULL,
                    "Note" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    "CreatedByUserId" INTEGER NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_ReglementsGroupes_TiersId" ON "ReglementsGroupes" ("TiersId");
                CREATE INDEX IF NOT EXISTS "IX_ReglementsGroupes_TiersId_Sens" ON "ReglementsGroupes" ("TiersId", "Sens");
                ALTER TABLE "PaiementsBonSortie" ADD COLUMN "ReglementGroupeId" INTEGER NULL;
                CREATE INDEX IF NOT EXISTS "IX_PaiementsBonSortie_ReglementGroupeId" ON "PaiementsBonSortie" ("ReglementGroupeId");
                ALTER TABLE "PaiementsBonAchat" ADD COLUMN "ReglementGroupeId" INTEGER NULL;
                CREATE INDEX IF NOT EXISTS "IX_PaiementsBonAchat_ReglementGroupeId" ON "PaiementsBonAchat" ("ReglementGroupeId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS "ReglementsGroupes";
                DROP INDEX IF EXISTS "IX_PaiementsBonSortie_ReglementGroupeId";
                DROP INDEX IF EXISTS "IX_PaiementsBonAchat_ReglementGroupeId";
                """);
            // SQLite cannot drop columns added with ALTER TABLE; leave ReglementGroupeId on downgrade.
        }
    }
}
