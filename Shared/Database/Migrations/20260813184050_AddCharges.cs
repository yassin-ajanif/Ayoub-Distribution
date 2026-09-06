using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: some DBs already have "Charges" without TypesCharges / migration history.
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
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TypesCharges_Nom" ON "TypesCharges" ("Nom");
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Charges" (
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
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Charges_Date" ON "Charges" ("Date");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Charges_TypeChargeId" ON "Charges" ("TypeChargeId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "TypesCharges");
        }
    }
}
