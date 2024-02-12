using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class First_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menu_Outlets_OutletId1",
                table: "Menu");

            migrationBuilder.DropIndex(
                name: "IX_Menu_OutletId1",
                table: "Menu");

            migrationBuilder.DropColumn(
                name: "OutletId1",
                table: "Menu");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_OutletId",
                table: "Menu",
                column: "OutletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_Outlets_OutletId",
                table: "Menu",
                column: "OutletId",
                principalTable: "Outlets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menu_Outlets_OutletId",
                table: "Menu");

            migrationBuilder.DropIndex(
                name: "IX_Menu_OutletId",
                table: "Menu");

            migrationBuilder.AddColumn<int>(
                name: "OutletId1",
                table: "Menu",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Menu_OutletId1",
                table: "Menu",
                column: "OutletId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_Outlets_OutletId1",
                table: "Menu",
                column: "OutletId1",
                principalTable: "Outlets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
