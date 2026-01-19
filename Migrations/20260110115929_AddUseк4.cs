using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddUseк4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "Users",
                columns: new[] { "Id", "IsAdmin", "PasswordHash", "Username" },
                values: new object[] { -2, true, "$2a$11$xQt0prrwlKU7wKsZFV64vOo884rwmg6EXmIcuS1etsF0Rl2kTtYna", "admin" });

            migrationBuilder.InsertData(
                schema: "public",
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "Username" },
                values: new object[] { -1, "$2a$11$PY4GpqrmgMexI9vb01KBKOEcFjl62wbM5nFtNMHYofP3S70OGWFee", "user" });
        }
    }
}
