using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFakulteTelafiAyarlariConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FakulteTelafiAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    MinKisimNo = table.Column<int>(type: "int", nullable: false),
                    MaxKisimNo = table.Column<int>(type: "int", nullable: false),
                    TelafiYapilamayacakGun = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelafiBaslamaSaati = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FakulteTelafiAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FakulteTelafiAyarlari_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FakulteTelafiAyarlari_FakulteId",
                table: "FakulteTelafiAyarlari",
                column: "FakulteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FakulteTelafiAyarlari");
        }
    }
}
