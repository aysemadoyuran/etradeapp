using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationRequestReason",
                table: "Licenses");

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "LicensePayments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "LicensePayments");

            migrationBuilder.AddColumn<string>(
                name: "CancellationRequestReason",
                table: "Licenses",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
