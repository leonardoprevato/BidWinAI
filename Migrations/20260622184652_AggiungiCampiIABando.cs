using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidWinAI.Migrations
{
    /// <inheritdoc />
    public partial class AggiungiCampiIABando : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalisiIA",
                table: "Bandi",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestoEstratto",
                table: "Bandi",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalisiIA",
                table: "Bandi");

            migrationBuilder.DropColumn(
                name: "TestoEstratto",
                table: "Bandi");
        }
    }
}
