using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyGasConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyGasConsumptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ElectricalStationId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Consumption = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyGasConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyGasConsumptions_ElectricalPowerStations_ElectricalStat~",
                        column: x => x.ElectricalStationId,
                        principalSchema: "public",
                        principalTable: "ElectricalPowerStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyGasConsumptions_ElectricalStationId_Date",
                schema: "public",
                table: "DailyGasConsumptions",
                columns: new[] { "ElectricalStationId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyGasConsumptions",
                schema: "public");
        }
    }
}
