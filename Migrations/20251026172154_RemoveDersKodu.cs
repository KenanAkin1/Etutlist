using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDersKodu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DersKodu",
                table: "Dersler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DersKodu",
                table: "Dersler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
