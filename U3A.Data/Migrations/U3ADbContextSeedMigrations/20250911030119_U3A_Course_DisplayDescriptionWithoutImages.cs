using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Course_DisplayDescriptionWithoutImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayDescriptionWithoutImages",
                table: "Course",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayDescriptionWithoutImages",
                table: "Course");
        }
    }
}
