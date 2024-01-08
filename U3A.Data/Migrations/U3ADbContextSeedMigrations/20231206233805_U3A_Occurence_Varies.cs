using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Occurence_Varies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Occurrence",
                columns: new[] { "ID", "Name", "ShortName" },
                values: new object[] { 999, "Unscheduled (Varies)", "Varies" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Occurrence",
                keyColumn: "ID",
                keyValue: 999);
        }
    }
}
