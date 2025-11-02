using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdAndBransiFromHoca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ad",
                table: "Hocalar");

            migrationBuilder.DropColumn(
                name: "Bransi",
                table: "Hocalar");

            migrationBuilder.DropColumn(
                name: "DersYuku",
                table: "Hocalar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ad",
                table: "Hocalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Bransi",
                table: "Hocalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DersYuku",
                table: "Hocalar",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
