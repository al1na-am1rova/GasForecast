using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddUser2 : Migration
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
                value: "$2a$11$5aGsW6MJe3XlWNuiTDrNQePZ9s2BaMPWo13WMDiIgfZ8pJ/syazKK");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -1,
                column: "PasswordHash",
                value: "$2a$11$WBgIlRFdjoJkxp3lZA998Oa8GwuMk5c.Js6BEwtrvO.ft9FHjWwL2");
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
                value: "$2a$11$Q9nDMd3sfRcAFDl8EHWLsu6qvICIY6ZuAv9Fw1rgx0NzuphkXQKjC");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "Users",
                keyColumn: "Id",
                keyValue: -1,
                column: "PasswordHash",
                value: "$2a$11$VM1WzSfTykozKUsJjHTzW.Yxqr98ePnSLU4tIqvJqLNGAVStAZrjW");
        }
    }
}
