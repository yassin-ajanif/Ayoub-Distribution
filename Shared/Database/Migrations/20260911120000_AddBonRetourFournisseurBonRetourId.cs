using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GestionCommerciale.Shared.Database;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260911120000_AddBonRetourFournisseurBonRetourId")]
    public class AddBonRetourFournisseurBonRetourId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BonsRetourFournisseurs" ADD COLUMN "BonRetourId" INTEGER NULL;
                CREATE INDEX IF NOT EXISTS "IX_BonsRetourFournisseurs_BonRetourId" ON "BonsRetourFournisseurs" ("BonRetourId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_BonsRetourFournisseurs_BonRetourId";
                """);
            // SQLite cannot drop columns added with ALTER TABLE; leave BonRetourId on downgrade.
        }
    }
}
