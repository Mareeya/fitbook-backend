using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitBook_App.Migrations
{
    /// <inheritdoc />
    public partial class SeedClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lookups",
                columns: new[] { "Id", "CreatedAt", "Type", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "class", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "yoga" },
                    { 2, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "class", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "cardio" },
                    { 3, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "class", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "strength" }
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Classes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Classes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Classes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Trainers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Trainers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Trainers",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
