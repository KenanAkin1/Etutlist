using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class AddAylikYedekListesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AylikYedekListeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    PersonelId = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AylikYedekListeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AylikYedekListeleri_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AylikYedekListeleri_PersonelId",
                table: "AylikYedekListeleri",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_AylikYedekListeleri_Yil_Ay_PersonelId",
                table: "AylikYedekListeleri",
                columns: new[] { "Yil", "Ay", "PersonelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AylikYedekListeleri");
        }
    }
}
