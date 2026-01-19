using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class Migration8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "temporaryPassword",
                schema: "public",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "temporaryPassword",
                schema: "public",
                table: "Users");
        }
    }
}
