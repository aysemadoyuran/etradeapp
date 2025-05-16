using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Data.Migrations
{
    /// <inheritdoc />
    public partial class LicencePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "LicensePayments",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "LicensePayments");
        }
    }
}
