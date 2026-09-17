using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisciplineApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIfThenPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IfThenPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IfCue = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ThenAction = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfThenPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IfThenPlans_UserId",
                table: "IfThenPlans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IfThenPlans");
        }
    }
}
