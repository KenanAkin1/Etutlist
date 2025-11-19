using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddOzelGrupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OzelGruplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GrupAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OzelGruplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OzelGrupUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OzelGrupId = table.Column<int>(type: "int", nullable: false),
                    PersonelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OzelGrupUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OzelGrupUyeleri_OzelGruplar_OzelGrupId",
                        column: x => x.OzelGrupId,
                        principalTable: "OzelGruplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OzelGrupUyeleri_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OzelGrupUyeleri_OzelGrupId_PersonelId",
                table: "OzelGrupUyeleri",
                columns: new[] { "OzelGrupId", "PersonelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OzelGrupUyeleri_PersonelId",
                table: "OzelGrupUyeleri",
                column: "PersonelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OzelGrupUyeleri");

            migrationBuilder.DropTable(
                name: "OzelGruplar");
        }
    }
}
