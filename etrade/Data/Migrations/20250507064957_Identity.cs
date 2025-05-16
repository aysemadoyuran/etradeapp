using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Data.Migrations
{
    /// <inheritdoc />
    public partial class Identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TenantCustomers",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DemoRequest",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomers_UserId",
                table: "TenantCustomers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantCustomers_AspNetUsers_UserId",
                table: "TenantCustomers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantCustomers_AspNetUsers_UserId",
                table: "TenantCustomers");

            migrationBuilder.DropIndex(
                name: "IX_TenantCustomers_UserId",
                table: "TenantCustomers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TenantCustomers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DemoRequest");
        }
    }
}
