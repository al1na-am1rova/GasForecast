using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class Uodate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPowerPercentage",
                table: "ElectricalPowerStations");

            migrationBuilder.AlterColumn<double>(
                name: "ConsumptionNorm",
                table: "ElectricalUnitPassports",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<double>(
                name: "GasConsumption",
                table: "ElectricalPowerStations",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ConsumptionNorm",
                table: "ElectricalUnitPassports",
                type: "decimal(18,6)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "GasConsumption",
                table: "ElectricalPowerStations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPowerPercentage",
                table: "ElectricalPowerStations",
                type: "decimal(3,0)",
                nullable: false,
                defaultValue: 0);
        }
    }
}
