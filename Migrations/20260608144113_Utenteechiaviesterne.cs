using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BidWinAI.Migrations
{
    /// <inheritdoc />
    public partial class Utenteechiaviesterne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UtenteId",
                table: "Bandi",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Utente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utente", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bandi_UtenteId",
                table: "Bandi",
                column: "UtenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bandi_Utente_UtenteId",
                table: "Bandi",
                column: "UtenteId",
                principalTable: "Utente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bandi_Utente_UtenteId",
                table: "Bandi");

            migrationBuilder.DropTable(
                name: "Utente");

            migrationBuilder.DropIndex(
                name: "IX_Bandi_UtenteId",
                table: "Bandi");

            migrationBuilder.DropColumn(
                name: "UtenteId",
                table: "Bandi");
        }
    }
}
