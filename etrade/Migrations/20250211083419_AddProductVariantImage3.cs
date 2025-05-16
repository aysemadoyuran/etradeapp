using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantImage3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "ColorImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ColorImages_ProductId",
                table: "ColorImages",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ColorImages_Products_ProductId",
                table: "ColorImages",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ColorImages_Products_ProductId",
                table: "ColorImages");

            migrationBuilder.DropIndex(
                name: "IX_ColorImages_ProductId",
                table: "ColorImages");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ColorImages");
        }
    }
}
