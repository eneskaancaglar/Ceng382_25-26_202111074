using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddCateringPackageAndEventOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventAddress",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EventDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Orders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GuestCount",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "MenuItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncludedItems",
                table: "MenuItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesDessert",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesDrinks",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesMainCourse",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesSnacks",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxGuestCount",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinGuestCount",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PackageCategory",
                table: "MenuItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceDetails",
                table: "MenuItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EventNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GuestCount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IncludedItems",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IncludesDessert",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IncludesDrinks",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IncludesMainCourse",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IncludesSnacks",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "MaxGuestCount",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "MinGuestCount",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "PackageCategory",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ServiceDetails",
                table: "MenuItems");
        }
    }
}
