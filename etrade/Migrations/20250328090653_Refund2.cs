using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class Refund2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_PaymentMethods_OrderId",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "RefundRequests");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId",
                table: "RefundRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_PaymentMethodId",
                table: "RefundRequests",
                column: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_PaymentMethods_PaymentMethodId",
                table: "RefundRequests",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_PaymentMethods_PaymentMethodId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_PaymentMethodId",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "RefundRequests");

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "RefundRequests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_PaymentMethods_OrderId",
                table: "RefundRequests",
                column: "OrderId",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
