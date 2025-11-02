using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDerslikAddKisimNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DersProgrami_Derslikler_DerslikId",
                table: "DersProgrami");

            migrationBuilder.DropForeignKey(
                name: "FK_TelafiDersler_Derslikler_DerslikId",
                table: "TelafiDersler");

            migrationBuilder.DropTable(
                name: "Derslikler");

            migrationBuilder.DropIndex(
                name: "IX_TelafiDersler_DerslikId",
                table: "TelafiDersler");

            migrationBuilder.DropIndex(
                name: "IX_DersProgrami_DerslikId",
                table: "DersProgrami");

            migrationBuilder.DropColumn(
                name: "DerslikId",
                table: "TelafiDersler");

            migrationBuilder.RenameColumn(
                name: "DerslikId",
                table: "DersProgrami",
                newName: "KisimNo");

            migrationBuilder.AddColumn<int>(
                name: "KisimNo",
                table: "TelafiDersler",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KisimNo",
                table: "TelafiDersler");

            migrationBuilder.RenameColumn(
                name: "KisimNo",
                table: "DersProgrami",
                newName: "DerslikId");

            migrationBuilder.AddColumn<int>(
                name: "DerslikId",
                table: "TelafiDersler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Derslikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    DerslikAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DerslikTuru = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kapasite = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Derslikler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Derslikler_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_DerslikId",
                table: "TelafiDersler",
                column: "DerslikId");

            migrationBuilder.CreateIndex(
                name: "IX_DersProgrami_DerslikId",
                table: "DersProgrami",
                column: "DerslikId");

            migrationBuilder.CreateIndex(
                name: "IX_Derslikler_FakulteId",
                table: "Derslikler",
                column: "FakulteId");

            migrationBuilder.AddForeignKey(
                name: "FK_DersProgrami_Derslikler_DerslikId",
                table: "DersProgrami",
                column: "DerslikId",
                principalTable: "Derslikler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TelafiDersler_Derslikler_DerslikId",
                table: "TelafiDersler",
                column: "DerslikId",
                principalTable: "Derslikler",
                principalColumn: "Id");
        }
    }
}
