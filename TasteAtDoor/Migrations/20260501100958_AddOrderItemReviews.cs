using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteAtDoor.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderItemReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    CatererId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuRating = table.Column<int>(type: "int", nullable: false),
                    CatererRating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemReviews_AspNetUsers_CatererId",
                        column: x => x.CatererId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemReviews_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemReviews_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemReviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemReviews_CatererId",
                table: "OrderItemReviews",
                column: "CatererId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemReviews_MenuItemId",
                table: "OrderItemReviews",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemReviews_OrderId",
                table: "OrderItemReviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemReviews_OrderItemId",
                table: "OrderItemReviews",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemReviews_UserId",
                table: "OrderItemReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemReviews");
        }
    }
}

