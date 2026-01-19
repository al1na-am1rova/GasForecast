using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddUser1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "Users",
                columns: new[] { "Id", "IsAdmin", "PasswordHash", "Username" },
                values: new object[] { -2, true, "$2a$11$Q9nDMd3sfRcAFDl8EHWLsu6qvICIY6ZuAv9Fw1rgx0NzuphkXQKjC", "admin" });

            migrationBuilder.InsertData(
                schema: "public",
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "Username" },
                values: new object[] { -1, "$2a$11$VM1WzSfTykozKUsJjHTzW.Yxqr98ePnSLU4tIqvJqLNGAVStAZrjW", "user" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
