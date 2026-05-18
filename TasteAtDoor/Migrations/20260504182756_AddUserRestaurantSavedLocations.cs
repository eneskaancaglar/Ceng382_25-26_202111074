using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRestaurantSavedLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Caterers");

            migrationBuilder.AddColumn<int>(
                name: "CatererId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Caterers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Caterers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Caterers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Caterers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AspNetUsers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CatererId",
                table: "MenuItems",
                column: "CatererId");

            migrationBuilder.CreateIndex(
                name: "IX_Caterers_ApplicationUserId",
                table: "Caterers",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Caterers_AspNetUsers_ApplicationUserId",
                table: "Caterers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caterers_CatererId",
                table: "MenuItems",
                column: "CatererId",
                principalTable: "Caterers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caterers_AspNetUsers_ApplicationUserId",
                table: "Caterers");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caterers_CatererId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CatererId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_Caterers_ApplicationUserId",
                table: "Caterers");

            migrationBuilder.DropColumn(
                name: "CatererId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Caterers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Caterers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Caterers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Caterers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Caterers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

