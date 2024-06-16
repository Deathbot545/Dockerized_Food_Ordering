using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Order_API.Migrations
{
    /// <inheritdoc />
    public partial class ExtraItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtraItems",
                table: "OrderDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraItems",
                table: "OrderDetails");
        }
    }
}
