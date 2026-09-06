using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixChargesRemoveLegacyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure TypesCharges exists (needed by FK).
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "TypesCharges" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_TypesCharges" PRIMARY KEY AUTOINCREMENT,
                    "Nom" TEXT NOT NULL,
                    "Actif" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    "CreatedByUserId" INTEGER NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TypesCharges_Nom" ON "TypesCharges" ("Nom");
                """);

            // Rebuild Charges without legacy FournisseurId / BeneficiaireLibre columns.
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "Charges_rebuild";

                CREATE TABLE "Charges_rebuild" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Charges" PRIMARY KEY AUTOINCREMENT,
                    "TypeChargeId" INTEGER NOT NULL,
                    "Date" TEXT NOT NULL,
                    "Libelle" TEXT NOT NULL,
                    "MontantTtc" TEXT NOT NULL,
                    "Note" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    "CreatedByUserId" INTEGER NULL,
                    CONSTRAINT "FK_Charges_TypesCharges_TypeChargeId"
                        FOREIGN KEY ("TypeChargeId") REFERENCES "TypesCharges" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Charges_rebuild" (
                    "Id", "TypeChargeId", "Date", "Libelle", "MontantTtc", "Note",
                    "CreatedAt", "UpdatedAt", "CreatedByUserId"
                )
                SELECT
                    "Id",
                    "TypeChargeId",
                    "Date",
                    "Libelle",
                    "MontantTtc",
                    COALESCE("Note", ''),
                    "CreatedAt",
                    "UpdatedAt",
                    "CreatedByUserId"
                FROM "Charges";

                DROP TABLE "Charges";
                ALTER TABLE "Charges_rebuild" RENAME TO "Charges";
                CREATE INDEX IF NOT EXISTS "IX_Charges_Date" ON "Charges" ("Date");
                CREATE INDEX IF NOT EXISTS "IX_Charges_TypeChargeId" ON "Charges" ("TypeChargeId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible schema cleanup.
        }
    }
}
