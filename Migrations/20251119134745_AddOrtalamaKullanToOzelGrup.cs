using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddOrtalamaKullanToOzelGrup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OrtalamaKullan",
                table: "OzelGruplar",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrtalamaKullan",
                table: "OzelGruplar");
        }
    }
}
