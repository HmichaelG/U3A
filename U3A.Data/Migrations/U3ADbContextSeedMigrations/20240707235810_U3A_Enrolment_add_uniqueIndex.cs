using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Enrolment_add_uniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrolment_TermID",
                table: "Enrolment");

            migrationBuilder.CreateIndex(
                name: "idxUniqueEnrolments",
                table: "Enrolment",
                columns: new[] { "TermID", "CourseID", "ClassID", "PersonID" },
                unique: true,
                filter: "IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idxUniqueEnrolments",
                table: "Enrolment");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_TermID",
                table: "Enrolment",
                column: "TermID");
        }
    }
}
