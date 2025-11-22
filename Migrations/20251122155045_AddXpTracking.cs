using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisciplineApp.Migrations
{
    /// <inheritdoc />
    public partial class AddXpTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JoinDate",
                table: "AspNetUsers",
                newName: "JoinedDate");

            migrationBuilder.AddColumn<bool>(
                name: "XpAwarded",
                table: "UserTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DailyXpEarned",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastXpResetDate",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XpAwarded",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "DailyXpEarned",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastXpResetDate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "JoinedDate",
                table: "AspNetUsers",
                newName: "JoinDate");
        }
    }
}
