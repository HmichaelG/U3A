using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_ScheduleCache_RevertToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gz_jsonClasses",
                table: "ScheduleCache");

            migrationBuilder.DropColumn(
                name: "gz_jsonEnrolments",
                table: "ScheduleCache");

            migrationBuilder.AddColumn<string>(
                name: "jsonClasses",
                table: "ScheduleCache",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "jsonEnrolments",
                table: "ScheduleCache",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "jsonClasses",
                table: "ScheduleCache");

            migrationBuilder.DropColumn(
                name: "jsonEnrolments",
                table: "ScheduleCache");

            migrationBuilder.AddColumn<byte[]>(
                name: "gz_jsonClasses",
                table: "ScheduleCache",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "gz_jsonEnrolments",
                table: "ScheduleCache",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
