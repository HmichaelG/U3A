using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Course_seperate_TermFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CourseFeeTerm1",
                table: "Course",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "Optional fee per term)");

            migrationBuilder.AddColumn<decimal>(
                name: "CourseFeeTerm2",
                table: "Course",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "Optional fee per term)");

            migrationBuilder.AddColumn<decimal>(
                name: "CourseFeeTerm3",
                table: "Course",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "Optional fee per term)");

            migrationBuilder.AddColumn<decimal>(
                name: "CourseFeeTerm4",
                table: "Course",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "Optional fee per term)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseFeeTerm1",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "CourseFeeTerm2",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "CourseFeeTerm3",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "CourseFeeTerm4",
                table: "Course");
        }
    }
}
