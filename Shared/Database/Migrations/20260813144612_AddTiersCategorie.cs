using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTiersCategorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                table: "Tiers",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Officiel");

            migrationBuilder.Sql(
                """
                UPDATE Tiers
                SET Categorie = 'Comptoir'
                WHERE Nom = 'Client Comptoire';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tiers_Categorie",
                table: "Tiers",
                sql: "Categorie IN ('Officiel', 'Comptoir')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tiers_Categorie",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "Categorie",
                table: "Tiers");
        }
    }
}
