using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDersprogram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FakulteId1",
                table: "Taburlar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GunlukDersSaatleri",
                table: "Fakulteler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taburlar_FakulteId1",
                table: "Taburlar",
                column: "FakulteId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Taburlar_Fakulteler_FakulteId1",
                table: "Taburlar",
                column: "FakulteId1",
                principalTable: "Fakulteler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Taburlar_Fakulteler_FakulteId1",
                table: "Taburlar");

            migrationBuilder.DropIndex(
                name: "IX_Taburlar_FakulteId1",
                table: "Taburlar");

            migrationBuilder.DropColumn(
                name: "FakulteId1",
                table: "Taburlar");

            migrationBuilder.DropColumn(
                name: "GunlukDersSaatleri",
                table: "Fakulteler");
        }
    }
}
