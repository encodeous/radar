using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Radar.Migrations
{
    public partial class AddTestResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    RanAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Long = table.Column<double>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Ping = table.Column<double>(type: "REAL", nullable: false),
                    Upload = table.Column<double>(type: "REAL", nullable: false),
                    Download = table.Column<double>(type: "REAL", nullable: false),
                    HasSucceeded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.RanAt);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");
        }
    }
}
