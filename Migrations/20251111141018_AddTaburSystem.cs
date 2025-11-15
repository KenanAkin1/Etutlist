using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddTaburSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FakulteTelafiAyarlari");

            migrationBuilder.CreateTable(
                name: "Taburlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    TaburAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinKisimNo = table.Column<int>(type: "int", nullable: false),
                    MaxKisimNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taburlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Taburlar_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaburTelafiAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaburId = table.Column<int>(type: "int", nullable: false),
                    TelafiYapilamayacakGun = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelafiBaslamaSaati = table.Column<int>(type: "int", nullable: false),
                    TelafiMaxBitisSaati = table.Column<int>(type: "int", nullable: false),
                    TelafiYapilamayacakDersSaatleri = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaburTelafiAyarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaburTelafiAyarlari_Taburlar_TaburId",
                        column: x => x.TaburId,
                        principalTable: "Taburlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Taburlar_FakulteId",
                table: "Taburlar",
                column: "FakulteId");

            migrationBuilder.CreateIndex(
                name: "IX_TaburTelafiAyarlari_TaburId",
                table: "TaburTelafiAyarlari",
                column: "TaburId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaburTelafiAyarlari");

            migrationBuilder.DropTable(
                name: "Taburlar");

            migrationBuilder.CreateTable(
                name: "FakulteTelafiAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    TelafiBaslamaSaati = table.Column<int>(type: "int", nullable: false),
                    TelafiYapilamayacakDersSaatleri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelafiYapilamayacakGun = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
    }
}
