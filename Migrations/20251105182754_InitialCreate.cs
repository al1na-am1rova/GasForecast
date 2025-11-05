using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "ElectricalPowerStations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActiveUnitsCount = table.Column<int>(type: "integer", nullable: false),
                    UnitType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LaunchDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricalPowerStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectricalUnitPassports",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnitType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EngineType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RatedPower = table.Column<int>(type: "integer", nullable: false),
                    StandartPower = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionNorm = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricalUnitPassports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricalPowerStations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ElectricalUnitPassports",
                schema: "public");
        }
    }
}
