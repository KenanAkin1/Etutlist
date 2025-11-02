using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddDerslerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DersId",
                table: "DersProgrami",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Dersler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DersKodu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dersler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DersProgrami_DersId",
                table: "DersProgrami",
                column: "DersId");

            migrationBuilder.AddForeignKey(
                name: "FK_DersProgrami_Dersler_DersId",
                table: "DersProgrami",
                column: "DersId",
                principalTable: "Dersler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DersProgrami_Dersler_DersId",
                table: "DersProgrami");

            migrationBuilder.DropTable(
                name: "Dersler");

            migrationBuilder.DropIndex(
                name: "IX_DersProgrami_DersId",
                table: "DersProgrami");

            migrationBuilder.DropColumn(
                name: "DersId",
                table: "DersProgrami");
        }
    }
}
