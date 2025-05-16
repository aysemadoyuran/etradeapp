using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class Discount2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountProduct_Discount_DiscountsId",
                table: "DiscountProduct");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscountProduct_Products_ProductsId",
                table: "DiscountProduct");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiscountProduct",
                table: "DiscountProduct");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Discount",
                table: "Discount");

            migrationBuilder.RenameTable(
                name: "DiscountProduct",
                newName: "ProductDiscounts");

            migrationBuilder.RenameTable(
                name: "Discount",
                newName: "Discounts");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountProduct_ProductsId",
                table: "ProductDiscounts",
                newName: "IX_ProductDiscounts_ProductsId");

            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductDiscounts",
                table: "ProductDiscounts",
                columns: new[] { "DiscountsId", "ProductsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Discounts",
                table: "Discounts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DiscountId",
                table: "Categories",
                column: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Discounts_DiscountId",
                table: "Categories",
                column: "DiscountId",
                principalTable: "Discounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDiscounts_Discounts_DiscountsId",
                table: "ProductDiscounts",
                column: "DiscountsId",
                principalTable: "Discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDiscounts_Products_ProductsId",
                table: "ProductDiscounts",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Discounts_DiscountId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductDiscounts_Discounts_DiscountsId",
                table: "ProductDiscounts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductDiscounts_Products_ProductsId",
                table: "ProductDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_Categories_DiscountId",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductDiscounts",
                table: "ProductDiscounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Discounts",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                table: "Categories");

            migrationBuilder.RenameTable(
                name: "ProductDiscounts",
                newName: "DiscountProduct");

            migrationBuilder.RenameTable(
                name: "Discounts",
                newName: "Discount");

            migrationBuilder.RenameIndex(
                name: "IX_ProductDiscounts_ProductsId",
                table: "DiscountProduct",
                newName: "IX_DiscountProduct_ProductsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscountProduct",
                table: "DiscountProduct",
                columns: new[] { "DiscountsId", "ProductsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Discount",
                table: "Discount",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountProduct_Discount_DiscountsId",
                table: "DiscountProduct",
                column: "DiscountsId",
                principalTable: "Discount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountProduct_Products_ProductsId",
                table: "DiscountProduct",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
