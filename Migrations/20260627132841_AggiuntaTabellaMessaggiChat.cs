using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BidWinAI.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntaTabellaMessaggiChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessaggiChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BandoId = table.Column<int>(type: "integer", nullable: false),
                    Testo = table.Column<string>(type: "text", nullable: false),
                    IsAi = table.Column<bool>(type: "boolean", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Elaborato = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessaggiChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessaggiChat_Bandi_BandoId",
                        column: x => x.BandoId,
                        principalTable: "Bandi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessaggiChat_BandoId",
                table: "MessaggiChat",
                column: "BandoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessaggiChat");
        }
    }
}
