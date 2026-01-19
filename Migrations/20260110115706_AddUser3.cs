using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddUser3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -2,
                column: "PasswordHash",
                value: "$2a$11$xQt0prrwlKU7wKsZFV64vOo884rwmg6EXmIcuS1etsF0Rl2kTtYna");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -1,
                column: "PasswordHash",
                value: "$2a$11$PY4GpqrmgMexI9vb01KBKOEcFjl62wbM5nFtNMHYofP3S70OGWFee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -2,
                column: "PasswordHash",
                value: "$2a$11$5aGsW6MJe3XlWNuiTDrNQePZ9s2BaMPWo13WMDiIgfZ8pJ/syazKK");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -1,
                column: "PasswordHash",
                value: "$2a$11$WBgIlRFdjoJkxp3lZA998Oa8GwuMk5c.Js6BEwtrvO.ft9FHjWwL2");
        }
    }
}
