using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitBook_App.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceAndCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_ClassId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "CheckedInCount",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeatsTaken",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "AttendanceStatus",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarkedByUserId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Source",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ClassId_StartAt",
                table: "Sessions",
                columns: new[] { "ClassId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartAt",
                table: "Sessions",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MarkedByUserId",
                table: "Bookings",
                column: "MarkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SessionId_AttendanceStatus",
                table: "Bookings",
                columns: new[] { "SessionId", "AttendanceStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId_CreatedAt",
                table: "Bookings",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_MarkedByUserId",
                table: "Bookings",
                column: "MarkedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_MarkedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ClassId_StartAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_StartAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MarkedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_SessionId_AttendanceStatus",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId_CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInCount",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SeatsTaken",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MarkedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ClassId",
                table: "Sessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");
        }
    }
}
