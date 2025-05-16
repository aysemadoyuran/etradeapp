using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class Basket2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemsBaskets_Colors_ColorId",
                table: "ItemsBaskets");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemsBaskets_Sizes_SizeId",
                table: "ItemsBaskets");

            migrationBuilder.DropIndex(
                name: "IX_ItemsBaskets_ColorId",
                table: "ItemsBaskets");

            migrationBuilder.DropIndex(
                name: "IX_ItemsBaskets_SizeId",
                table: "ItemsBaskets");

            migrationBuilder.DropColumn(
                name: "ColorId",
                table: "ItemsBaskets");

            migrationBuilder.DropColumn(
                name: "SizeId",
                table: "ItemsBaskets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColorId",
                table: "ItemsBaskets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SizeId",
                table: "ItemsBaskets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ItemsBaskets_ColorId",
                table: "ItemsBaskets",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsBaskets_SizeId",
                table: "ItemsBaskets",
                column: "SizeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsBaskets_Colors_ColorId",
                table: "ItemsBaskets",
                column: "ColorId",
                principalTable: "Colors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsBaskets_Sizes_SizeId",
                table: "ItemsBaskets",
                column: "SizeId",
                principalTable: "Sizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
