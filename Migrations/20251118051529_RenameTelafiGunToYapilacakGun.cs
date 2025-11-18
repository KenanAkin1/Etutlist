using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class RenameTelafiGunToYapilacakGun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelafiYapilamayacakGun",
                table: "TaburTelafiAyarlari",
                newName: "TelafiYapilacakGun");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelafiYapilacakGun",
                table: "TaburTelafiAyarlari",
                newName: "TelafiYapilamayacakGun");
        }
    }
}
