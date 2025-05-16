using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace etrade.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteBehaviorNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Address_AspNetUsers_UserId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Districts_SemtId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Ilceler_IlceId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Iller_IlId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Streets_MahalleId",
                table: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_AspNetUsers_UserId",
                table: "Address",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Districts_SemtId",
                table: "Address",
                column: "SemtId",
                principalTable: "Districts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Ilceler_IlceId",
                table: "Address",
                column: "IlceId",
                principalTable: "Ilceler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Iller_IlId",
                table: "Address",
                column: "IlId",
                principalTable: "Iller",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Streets_MahalleId",
                table: "Address",
                column: "MahalleId",
                principalTable: "Streets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Address_AspNetUsers_UserId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Districts_SemtId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Ilceler_IlceId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Iller_IlId",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_Streets_MahalleId",
                table: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_AspNetUsers_UserId",
                table: "Address",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Districts_SemtId",
                table: "Address",
                column: "SemtId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Ilceler_IlceId",
                table: "Address",
                column: "IlceId",
                principalTable: "Ilceler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Iller_IlId",
                table: "Address",
                column: "IlId",
                principalTable: "Iller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Streets_MahalleId",
                table: "Address",
                column: "MahalleId",
                principalTable: "Streets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
