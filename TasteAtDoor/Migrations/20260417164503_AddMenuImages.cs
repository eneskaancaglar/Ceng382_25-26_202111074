using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "MenuItems",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "MenuItems");
        }
    }
}

