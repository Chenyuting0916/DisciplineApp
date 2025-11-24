using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisciplineApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFocusAndCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoldCoins",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalFocusMinutes",
                table: "AspNetUsers",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "FocusSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<double>(type: "REAL", nullable: false),
                    TaskTag = table.Column<string>(type: "TEXT", nullable: true),
                    IsPomodoro = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FocusSessions");

            migrationBuilder.DropColumn(
                name: "GoldCoins",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalFocusMinutes",
                table: "AspNetUsers");
        }
    }
}
