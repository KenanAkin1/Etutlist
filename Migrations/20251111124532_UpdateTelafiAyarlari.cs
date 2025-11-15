using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTelafiAyarlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxKisimNo",
                table: "FakulteTelafiAyarlari");

            migrationBuilder.DropColumn(
                name: "MinKisimNo",
                table: "FakulteTelafiAyarlari");

            migrationBuilder.AddColumn<string>(
                name: "TelafiYapilamayacakDersSaatleri",
                table: "FakulteTelafiAyarlari",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelafiYapilamayacakDersSaatleri",
                table: "FakulteTelafiAyarlari");

            migrationBuilder.AddColumn<int>(
                name: "MaxKisimNo",
                table: "FakulteTelafiAyarlari",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinKisimNo",
                table: "FakulteTelafiAyarlari",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
