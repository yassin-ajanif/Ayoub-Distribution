using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonAchat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonsAchat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    FournisseurId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstPayee = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTtc = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsAchat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonAchatLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonAchatId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_BonAchatLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonAchatLignes_BonsAchat_BonAchatId",
                        column: x => x.BonAchatId,
                        principalTable: "BonsAchat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaiementsBonAchat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonAchatId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_PaiementsBonAchat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaiementsBonAchat_BonsAchat_BonAchatId",
                        column: x => x.BonAchatId,
                        principalTable: "BonsAchat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonAchatLignes_BonAchatId",
                table: "BonAchatLignes",
                column: "BonAchatId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsBonAchat_BonAchatId",
                table: "PaiementsBonAchat",
                column: "BonAchatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonAchatLignes");

            migrationBuilder.DropTable(
                name: "PaiementsBonAchat");

            migrationBuilder.DropTable(
                name: "BonsAchat");
        }
    }
}
