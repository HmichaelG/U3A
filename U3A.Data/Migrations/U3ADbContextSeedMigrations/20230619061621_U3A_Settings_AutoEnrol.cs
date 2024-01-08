using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Settings_AutoEnrol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AutoEnrolAllocationDay",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                comment: "The day on which random allocation occurs",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "The Sunday in January on which random allocation occurs");

            migrationBuilder.AddColumn<int>(
                name: "AutoEnrolAllocationOccurs",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "How often random allocation occurs");

            migrationBuilder.AddColumn<int>(
                name: "AutoEnrolAllocationWeek",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: -2,
                comment: "The week prior to term start in which random allocation occurs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoEnrolAllocationOccurs",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AutoEnrolAllocationWeek",
                table: "SystemSettings");

            migrationBuilder.AlterColumn<int>(
                name: "AutoEnrolAllocationDay",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                comment: "The Sunday in January on which random allocation occurs",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "The day on which random allocation occurs");
        }
    }
}
