using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Data.Migrations
{
    /// <inheritdoc />
    public partial class Dem2o : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemoRequest_TenantCustomers_TenantCustomerId",
                table: "DemoRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DemoRequest",
                table: "DemoRequest");

            migrationBuilder.RenameTable(
                name: "DemoRequest",
                newName: "DemoRequests");

            migrationBuilder.RenameIndex(
                name: "IX_DemoRequest_TenantCustomerId",
                table: "DemoRequests",
                newName: "IX_DemoRequests_TenantCustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DemoRequests",
                table: "DemoRequests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DemoRequests_TenantCustomers_TenantCustomerId",
                table: "DemoRequests",
                column: "TenantCustomerId",
                principalTable: "TenantCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemoRequests_TenantCustomers_TenantCustomerId",
                table: "DemoRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DemoRequests",
                table: "DemoRequests");

            migrationBuilder.RenameTable(
                name: "DemoRequests",
                newName: "DemoRequest");

            migrationBuilder.RenameIndex(
                name: "IX_DemoRequests_TenantCustomerId",
                table: "DemoRequest",
                newName: "IX_DemoRequest_TenantCustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DemoRequest",
                table: "DemoRequest",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DemoRequest_TenantCustomers_TenantCustomerId",
                table: "DemoRequest",
                column: "TenantCustomerId",
                principalTable: "TenantCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
