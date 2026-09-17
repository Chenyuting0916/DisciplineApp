using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisciplineApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSanctuaryShopAndIntentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquippedTitle",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnedItems",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StreakFreezeTokens",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThemeKey",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DailyIntentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Vow = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    OneThing = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyIntentions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyIntentions_UserId_Date",
                table: "DailyIntentions",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyIntentions");

            migrationBuilder.DropColumn(
                name: "EquippedTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedItems",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StreakFreezeTokens",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ThemeKey",
                table: "AspNetUsers");
        }
    }
}
