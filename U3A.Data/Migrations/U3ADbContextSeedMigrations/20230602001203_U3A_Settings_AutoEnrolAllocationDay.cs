using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Settings_AutoEnrolAllocationDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoEnrolAllocationDay",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 2,
                comment: "The Sunday in January on which random allocation occurs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoEnrolAllocationDay",
                table: "SystemSettings");
        }
    }
}
