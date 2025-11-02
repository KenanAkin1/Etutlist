using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etutlist.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedStructureNoBolum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fakulteler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fakulteler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OzelGunler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OzelGunler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personeller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rutbe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YedekSayisi = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PazarSayisi = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    OzelGunSayisi = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HaftaIciSayisi = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personeller", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Hocalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rutbe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bransi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DersYuku = table.Column<int>(type: "int", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    FakulteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hocalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hocalar_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etutler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonelId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SinifNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etutler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etutler_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mazeretler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonelId = table.Column<int>(type: "int", nullable: false),
                    Baslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mazeretler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mazeretler_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DersProgrami",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    DerslikId = table.Column<int>(type: "int", nullable: false),
                    HocaId = table.Column<int>(type: "int", nullable: false),
                    DersAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DersKodu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DersGunu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DersSaati = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersProgrami", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DersProgrami_Derslikler_DerslikId",
                        column: x => x.DerslikId,
                        principalTable: "Derslikler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DersProgrami_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DersProgrami_Hocalar_HocaId",
                        column: x => x.HocaId,
                        principalTable: "Hocalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TelafiDersler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersProgramiId = table.Column<int>(type: "int", nullable: false),
                    YedekHocaId = table.Column<int>(type: "int", nullable: false),
                    FakulteId = table.Column<int>(type: "int", nullable: false),
                    DerslikId = table.Column<int>(type: "int", nullable: false),
                    TelafiTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslangicSaat = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaat = table.Column<TimeSpan>(type: "time", nullable: false),
                    TelafiTuru = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelafiNedeni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Onaylandi = table.Column<bool>(type: "bit", nullable: false),
                    HocaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelafiDersler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelafiDersler_DersProgrami_DersProgramiId",
                        column: x => x.DersProgramiId,
                        principalTable: "DersProgrami",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelafiDersler_Derslikler_DerslikId",
                        column: x => x.DerslikId,
                        principalTable: "Derslikler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelafiDersler_Fakulteler_FakulteId",
                        column: x => x.FakulteId,
                        principalTable: "Fakulteler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelafiDersler_Hocalar_HocaId",
                        column: x => x.HocaId,
                        principalTable: "Hocalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelafiDersler_Hocalar_YedekHocaId",
                        column: x => x.YedekHocaId,
                        principalTable: "Hocalar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Derslikler_FakulteId",
                table: "Derslikler",
                column: "FakulteId");

            migrationBuilder.CreateIndex(
                name: "IX_DersProgrami_DerslikId",
                table: "DersProgrami",
                column: "DerslikId");

            migrationBuilder.CreateIndex(
                name: "IX_DersProgrami_FakulteId",
                table: "DersProgrami",
                column: "FakulteId");

            migrationBuilder.CreateIndex(
                name: "IX_DersProgrami_HocaId",
                table: "DersProgrami",
                column: "HocaId");

            migrationBuilder.CreateIndex(
                name: "IX_Etutler_PersonelId",
                table: "Etutler",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Hocalar_FakulteId",
                table: "Hocalar",
                column: "FakulteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mazeretler_PersonelId",
                table: "Mazeretler",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_DerslikId",
                table: "TelafiDersler",
                column: "DerslikId");

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_DersProgramiId",
                table: "TelafiDersler",
                column: "DersProgramiId");

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_FakulteId",
                table: "TelafiDersler",
                column: "FakulteId");

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_HocaId",
                table: "TelafiDersler",
                column: "HocaId");

            migrationBuilder.CreateIndex(
                name: "IX_TelafiDersler_YedekHocaId",
                table: "TelafiDersler",
                column: "YedekHocaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Etutler");

            migrationBuilder.DropTable(
                name: "Mazeretler");

            migrationBuilder.DropTable(
                name: "OzelGunler");

            migrationBuilder.DropTable(
                name: "TelafiDersler");

            migrationBuilder.DropTable(
                name: "Personeller");

            migrationBuilder.DropTable(
                name: "DersProgrami");

            migrationBuilder.DropTable(
                name: "Derslikler");

            migrationBuilder.DropTable(
                name: "Hocalar");

            migrationBuilder.DropTable(
                name: "Fakulteler");
        }
    }
}
