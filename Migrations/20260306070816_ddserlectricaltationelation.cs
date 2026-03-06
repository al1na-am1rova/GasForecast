using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GasForecast.Migrations
{
    /// <inheritdoc />
    public partial class ddserlectricaltationelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserElectricalStations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ElectricalStationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserElectricalStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserElectricalStations_ElectricalPowerStations_ElectricalSt~",
                        column: x => x.ElectricalStationId,
                        principalSchema: "public",
                        principalTable: "ElectricalPowerStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserElectricalStations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserElectricalStations_ElectricalStationId",
                schema: "public",
                table: "UserElectricalStations",
                column: "ElectricalStationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserElectricalStations_UserId_ElectricalStationId",
                schema: "public",
                table: "UserElectricalStations",
                columns: new[] { "UserId", "ElectricalStationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserElectricalStations",
                schema: "public");
        }
    }
}
