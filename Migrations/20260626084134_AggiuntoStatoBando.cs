using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidWinAI.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoStatoBando : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TestoEstratto",
                table: "Bandi",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnalisiIA",
                table: "Bandi",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessaggioErrore",
                table: "Bandi",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stato",
                table: "Bandi",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessaggioErrore",
                table: "Bandi");

            migrationBuilder.DropColumn(
                name: "Stato",
                table: "Bandi");

            migrationBuilder.AlterColumn<string>(
                name: "TestoEstratto",
                table: "Bandi",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AnalisiIA",
                table: "Bandi",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
