using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddHocaDersRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HocaDersler",
                columns: table => new
                {
                    HocaId = table.Column<int>(type: "int", nullable: false),
                    DersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocaDersler", x => new { x.HocaId, x.DersId });
                    table.ForeignKey(
                        name: "FK_HocaDersler_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HocaDersler_Hocalar_HocaId",
                        column: x => x.HocaId,
                        principalTable: "Hocalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HocaDersler_DersId",
                table: "HocaDersler",
                column: "DersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HocaDersler");
        }
    }
}
