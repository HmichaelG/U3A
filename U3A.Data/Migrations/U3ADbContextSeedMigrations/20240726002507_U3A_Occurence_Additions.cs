using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Occurence_Additions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Occurrence",
                columns: new[] { "ID", "Name", "ShortName" },
                values: new object[,]
                {
                    { 11, "1st & 3rd Week of Month", "Weeks 1 & 3" },
                    { 12, "2nd & 4th Week of Month", "Weeks 2 & 4" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Occurrence",
                keyColumn: "ID",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Occurrence",
                keyColumn: "ID",
                keyValue: 12);
        }
    }
}
