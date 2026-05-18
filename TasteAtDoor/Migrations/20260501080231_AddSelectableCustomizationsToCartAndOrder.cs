using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectableCustomizationsToCartAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "FinalUnitPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseUnitPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "OrderItemCustomizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    CustomizationGroupId = table.Column<int>(type: "int", nullable: false),
                    CustomizationOptionId = table.Column<int>(type: "int", nullable: false),
                    GroupTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceChange = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemCustomizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemCustomizations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCustomizations_OrderItemId",
                table: "OrderItemCustomizations",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemCustomizations");

            migrationBuilder.DropColumn(
                name: "BaseUnitPrice",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "FinalUnitPrice",
                table: "OrderItems",
                newName: "UnitPrice");
        }
    }
}

