using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "BonsPreparation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    DevisId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstPayee = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTtc = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    BonCommandeReference = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsPreparation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonPreparationLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonPreparationId = table.Column<int>(type: "INTEGER", nullable: false),
                    BonLivraisonId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    Conditionnement = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonPreparationLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonPreparationLignes_BonsLivraison_BonLivraisonId",
                        column: x => x.BonLivraisonId,
                        principalTable: "BonsLivraison",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BonPreparationLignes_BonsPreparation_BonPreparationId",
                        column: x => x.BonPreparationId,
                        principalTable: "BonsPreparation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaiementsBonPreparation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonPreparationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsBonPreparation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaiementsBonPreparation_BonsPreparation_BonPreparationId",
                        column: x => x.BonPreparationId,
                        principalTable: "BonsPreparation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_BonPreparationLignes_BonPreparationId",
                table: "BonPreparationLignes",
                column: "BonPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsBonPreparation_BonPreparationId",
                table: "PaiementsBonPreparation",
                column: "BonPreparationId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsCommandeClient_BonsPreparation_BonPreparationId",
                table: "BonsCommandeClient");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsPreparation_BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropTable(
                name: "BonPreparationLignes");

            migrationBuilder.DropTable(
                name: "PaiementsBonPreparation");

            migrationBuilder.DropTable(
                name: "BonsPreparation");

            migrationBuilder.DropIndex(
                name: "IX_BonsLivraison_BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsCommandeClient_BonPreparationId",
                table: "BonsCommandeClient");

            migrationBuilder.DropColumn(
                name: "BonPreparationId",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "BonPreparationId",
                table: "BonsCommandeClient");
        }
    }
}
