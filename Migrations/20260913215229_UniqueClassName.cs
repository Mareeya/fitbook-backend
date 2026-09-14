using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitBook_App.Migrations
{
    /// <inheritdoc />
    public partial class UniqueClassName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH Duplicates AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) AS RowNum
                    FROM Classes
                )
                DELETE FROM Duplicates WHERE RowNum > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Name",
                table: "Classes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_Name",
                table: "Classes");
        }
    }
}
