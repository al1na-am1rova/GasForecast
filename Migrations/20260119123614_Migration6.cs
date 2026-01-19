using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class Migration6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSessionTime",
                schema: "public",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSessionTime",
                schema: "public",
                table: "Users");
        }
    }
}
