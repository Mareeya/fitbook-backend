using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitBook_App.Migrations
{
    /// <inheritdoc />
    public partial class TrainerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Bookings;");
            migrationBuilder.Sql("DELETE FROM Sessions;");
            migrationBuilder.Sql("DELETE FROM Classes;");
            migrationBuilder.Sql("DELETE FROM Trainers;");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Trainers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_Users_UserId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Trainers");

            migrationBuilder.InsertData(
                table: "Trainers",
                columns: new[] { "Id", "CreatedAt", "Name", "Specialty", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Sara Khan", "Yoga", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Ali Raza", "Cardio", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Hina Malik", "Strength", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Classes",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "InitCapacity", "Name", "TrainerId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), 20, "Morning Yoga", 1, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 2, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), 15, "HIIT Cardio", 2, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 3, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), 12, "Strength Basics", 3, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}
