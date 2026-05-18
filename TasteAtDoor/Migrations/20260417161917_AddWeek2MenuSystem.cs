using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddWeek2MenuSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caterers_CatererId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CatererId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CatererId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MenuItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MenuItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CaretakerId",
                table: "MenuItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_AspNetUsers_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_AspNetUsers_CaretakerId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CaretakerId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CaretakerId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "CatererId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CatererId",
                table: "MenuItems",
                column: "CatererId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caterers_CatererId",
                table: "MenuItems",
                column: "CatererId",
                principalTable: "Caterers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

