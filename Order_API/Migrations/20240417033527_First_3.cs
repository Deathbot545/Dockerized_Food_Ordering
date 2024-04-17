using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Order_API.Migrations
{
    /// <inheritdoc />
    public partial class First_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_MenuItemData_MenuItemId",
                table: "OrderDetails");

            migrationBuilder.DropTable(
                name: "MenuItemData");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_MenuItemId",
                table: "OrderDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    MenuCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_MenuItemId",
                table: "OrderDetails",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_MenuItemData_MenuItemId",
                table: "OrderDetails",
                column: "MenuItemId",
                principalTable: "MenuItemData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
