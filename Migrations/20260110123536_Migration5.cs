using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class Migration5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                schema: "public",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "public",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "False");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "public",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                schema: "public",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
