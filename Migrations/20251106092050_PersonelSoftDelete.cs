using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class PersonelSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Etutler_Personeller_PersonelId",
                table: "Etutler");

            migrationBuilder.AlterColumn<int>(
                name: "PersonelId",
                table: "Etutler",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "PersonelAd",
                table: "Etutler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonelRutbe",
                table: "Etutler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Etutler_Personeller_PersonelId",
                table: "Etutler",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Etutler_Personeller_PersonelId",
                table: "Etutler");

            migrationBuilder.DropColumn(
                name: "PersonelAd",
                table: "Etutler");

            migrationBuilder.DropColumn(
                name: "PersonelRutbe",
                table: "Etutler");

            migrationBuilder.AlterColumn<int>(
                name: "PersonelId",
                table: "Etutler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Etutler_Personeller_PersonelId",
                table: "Etutler",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
