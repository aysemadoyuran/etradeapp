using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class Basket3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "ItemsBaskets",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "ItemsBaskets");
        }
    }
}
