using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectricalPowerStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActiveUnitsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LaunchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GasConsumption = table.Column<double>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricalPowerStations", x => x.Id);
                    table.UniqueConstraint("AK_ElectricalPowerStations_UnitType", x => x.UnitType);
                });

            migrationBuilder.CreateTable(
                name: "ElectricalUnitPassports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EngineType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RatedPower = table.Column<int>(type: "INTEGER", nullable: false),
                    StandartPower = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsumptionNorm = table.Column<double>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricalUnitPassports", x => x.Id);
                    table.UniqueConstraint("AK_ElectricalUnitPassports_UnitType", x => x.UnitType);
                });

            migrationBuilder.CreateTable(
                name: "ElectricalUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CurrentPowerPercentage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricalUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricalUnits_ElectricalPowerStations_UnitType",
                        column: x => x.UnitType,
                        principalTable: "ElectricalPowerStations",
                        principalColumn: "UnitType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElectricalUnits_ElectricalUnitPassports_UnitType",
                        column: x => x.UnitType,
                        principalTable: "ElectricalUnitPassports",
                        principalColumn: "UnitType",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectricalUnits_UnitType",
                table: "ElectricalUnits",
                column: "UnitType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricalUnits");

            migrationBuilder.DropTable(
                name: "ElectricalPowerStations");

            migrationBuilder.DropTable(
                name: "ElectricalUnitPassports");
        }
    }
}
