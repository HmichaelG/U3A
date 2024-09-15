using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class TdB_MCSchedule_AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantIdentifier",
                table: "MultiCampusSchedule",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_MultiCampusSchedule_TenantIdentifier_TermId_ClassID",
                table: "MultiCampusSchedule",
                columns: new[] { "TenantIdentifier", "TermId", "ClassID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MultiCampusSchedule_TenantIdentifier_TermId_ClassID",
                table: "MultiCampusSchedule");

            migrationBuilder.AlterColumn<string>(
                name: "TenantIdentifier",
                table: "MultiCampusSchedule",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
