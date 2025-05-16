using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCodeAndIsOkayToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Complete",
                table: "Products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "Products",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Complete",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "Products");
        }
    }
}
