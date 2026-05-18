using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "MenuItems",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "MenuItems",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MenuItems");
        }
    }
}

