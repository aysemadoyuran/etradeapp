using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class MahalleSemtAdd11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mahalleler");

            migrationBuilder.DropTable(
                name: "Semtler");

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Semtid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ilceid = table.Column<int>(type: "int", nullable: false),
                    SemtAdi = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Semtid);
                    table.ForeignKey(
                        name: "FK_Districts_Ilceler_Ilceid",
                        column: x => x.Ilceid,
                        principalTable: "Ilceler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Streets",
                columns: table => new
                {
                    Mahalleid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Semtid = table.Column<int>(type: "int", nullable: false),
                    MahalleAdi = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streets", x => x.Mahalleid);
                    table.ForeignKey(
                        name: "FK_Streets_Districts_Semtid",
                        column: x => x.Semtid,
                        principalTable: "Districts",
                        principalColumn: "Semtid",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Ilceid",
                table: "Districts",
                column: "Ilceid");

            migrationBuilder.CreateIndex(
                name: "IX_Streets_Semtid",
                table: "Streets",
                column: "Semtid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Streets");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.CreateTable(
                name: "Semtler",
                columns: table => new
                {
                    Ilceid = table.Column<int>(type: "int", nullable: false),
                    Semt = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Semtid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semtler", x => x.Ilceid);
                    table.ForeignKey(
                        name: "FK_Semtler_Ilceler_Ilceid",
                        column: x => x.Ilceid,
                        principalTable: "Ilceler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mahalleler",
                columns: table => new
                {
                    Mahalleid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Semtid = table.Column<int>(type: "int", nullable: false),
                    Mahalle1 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mahalleler", x => x.Mahalleid);
                    table.ForeignKey(
                        name: "FK_Mahalleler_Semtler_Semtid",
                        column: x => x.Semtid,
                        principalTable: "Semtler",
                        principalColumn: "Ilceid",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Mahalleler_Semtid",
                table: "Mahalleler",
                column: "Semtid");
        }
    }
}
