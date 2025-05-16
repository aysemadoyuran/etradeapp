using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountTable9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Discounts");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Discounts",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Discounts",
                newName: "EndDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                table: "Discounts",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "EndDateTime",
                table: "Discounts",
                newName: "EndDate");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "Discounts",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "Discounts",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
