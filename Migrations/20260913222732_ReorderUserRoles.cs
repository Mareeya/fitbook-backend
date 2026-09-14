using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitBook_App.Migrations
{
    /// <inheritdoc />
    public partial class ReorderUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET Role = Role + 10;");
            migrationBuilder.Sql("UPDATE Users SET Role = 1 WHERE Role = 13;");
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role = 14;");
            migrationBuilder.Sql("UPDATE Users SET Role = 3 WHERE Role = 11 OR Role = 12;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET Role = Role + 10;");
            migrationBuilder.Sql("UPDATE Users SET Role = 1 WHERE Role = 13;");
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role = 12;");
            migrationBuilder.Sql("UPDATE Users SET Role = 3 WHERE Role = 11;");
            migrationBuilder.Sql("UPDATE Users SET Role = 4 WHERE Role = 12;");
        }
    }
}
