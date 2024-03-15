using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Settings_DataRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetainAttendanceForYears",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "RetainEnrolmentForYears",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "RetainFinancialsForYears",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "RetainRegistrationsNeverCompletedForDays",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "RetainUnfinancialPersonsForYears",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetainAttendanceForYears",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RetainEnrolmentForYears",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RetainFinancialsForYears",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RetainRegistrationsNeverCompletedForDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RetainUnfinancialPersonsForYears",
                table: "SystemSettings");
        }
    }
}
