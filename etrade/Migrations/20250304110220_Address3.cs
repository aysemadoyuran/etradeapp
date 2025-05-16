using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class Address3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Districts_Ilceler_Ilceid",
                table: "Districts");

            migrationBuilder.DropForeignKey(
                name: "FK_Streets_Districts_Semtid",
                table: "Streets");

            migrationBuilder.RenameColumn(
                name: "Semtid",
                table: "Streets",
                newName: "SemtId");

            migrationBuilder.RenameColumn(
                name: "Mahalleid",
                table: "Streets",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Streets_Semtid",
                table: "Streets",
                newName: "IX_Streets_SemtId");

            migrationBuilder.RenameColumn(
                name: "Ilceid",
                table: "Districts",
                newName: "IlceId");

            migrationBuilder.RenameColumn(
                name: "Semtid",
                table: "Districts",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Districts_Ilceid",
                table: "Districts",
                newName: "IX_Districts_IlceId");

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IlId = table.Column<sbyte>(type: "TINYINT", nullable: false),
                    IlceId = table.Column<int>(type: "int", nullable: false),
                    SemtId = table.Column<int>(type: "int", nullable: false),
                    MahalleId = table.Column<int>(type: "int", nullable: false),
                    AcikAdres = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Address_Districts_SemtId",
                        column: x => x.SemtId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Address_Ilceler_IlceId",
                        column: x => x.IlceId,
                        principalTable: "Ilceler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Address_Iller_IlId",
                        column: x => x.IlId,
                        principalTable: "Iller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Address_Streets_MahalleId",
                        column: x => x.MahalleId,
                        principalTable: "Streets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Address_IlceId",
                table: "Address",
                column: "IlceId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_IlId",
                table: "Address",
                column: "IlId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_MahalleId",
                table: "Address",
                column: "MahalleId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_SemtId",
                table: "Address",
                column: "SemtId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_UserId",
                table: "Address",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Districts_Ilceler_IlceId",
                table: "Districts",
                column: "IlceId",
                principalTable: "Ilceler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Streets_Districts_SemtId",
                table: "Streets",
                column: "SemtId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Districts_Ilceler_IlceId",
                table: "Districts");

            migrationBuilder.DropForeignKey(
                name: "FK_Streets_Districts_SemtId",
                table: "Streets");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.RenameColumn(
                name: "SemtId",
                table: "Streets",
                newName: "Semtid");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Streets",
                newName: "Mahalleid");

            migrationBuilder.RenameIndex(
                name: "IX_Streets_SemtId",
                table: "Streets",
                newName: "IX_Streets_Semtid");

            migrationBuilder.RenameColumn(
                name: "IlceId",
                table: "Districts",
                newName: "Ilceid");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Districts",
                newName: "Semtid");

            migrationBuilder.RenameIndex(
                name: "IX_Districts_IlceId",
                table: "Districts",
                newName: "IX_Districts_Ilceid");

            migrationBuilder.AddForeignKey(
                name: "FK_Districts_Ilceler_Ilceid",
                table: "Districts",
                column: "Ilceid",
                principalTable: "Ilceler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Streets_Districts_Semtid",
                table: "Streets",
                column: "Semtid",
                principalTable: "Districts",
                principalColumn: "Semtid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
