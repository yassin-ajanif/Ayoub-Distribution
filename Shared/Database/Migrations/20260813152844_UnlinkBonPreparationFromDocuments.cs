using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class UnlinkBonPreparationFromDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonPreparationLignes_BonsLivraison_BonLivraisonId",
                table: "BonPreparationLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsCommandeClient_BonsPreparation_BonPreparationId",
                table: "BonsCommandeClient");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsPreparation_BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsLivraison_BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsCommandeClient_BonPreparationId",
                table: "BonsCommandeClient");

            migrationBuilder.DropIndex(
                name: "IX_BonPreparationLignes_BonLivraisonId",
                table: "BonPreparationLignes");

            migrationBuilder.DropColumn(
                name: "BonCommandeReference",
                table: "BonsPreparation");

            migrationBuilder.DropColumn(
                name: "DevisId",
                table: "BonsPreparation");

            migrationBuilder.DropColumn(
                name: "BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "BonPreparationId",
                table: "BonsCommandeClient");

            migrationBuilder.DropColumn(
                name: "BonLivraisonId",
                table: "BonPreparationLignes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BonCommandeReference",
                table: "BonsPreparation",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DevisId",
                table: "BonsPreparation",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BonPreparationId",
                table: "BonsLivraison",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BonPreparationId",
                table: "BonsCommandeClient",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BonLivraisonId",
                table: "BonPreparationLignes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_BonPreparationId",
                table: "BonsLivraison",
                column: "BonPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCommandeClient_BonPreparationId",
                table: "BonsCommandeClient",
                column: "BonPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonPreparationLignes_BonLivraisonId",
                table: "BonPreparationLignes",
                column: "BonLivraisonId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonPreparationLignes_BonsLivraison_BonLivraisonId",
                table: "BonPreparationLignes",
                column: "BonLivraisonId",
                principalTable: "BonsLivraison",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsCommandeClient_BonsPreparation_BonPreparationId",
                table: "BonsCommandeClient",
                column: "BonPreparationId",
                principalTable: "BonsPreparation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_BonsPreparation_BonPreparationId",
                table: "BonsLivraison",
                column: "BonPreparationId",
                principalTable: "BonsPreparation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
