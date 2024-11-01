using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Enrolment_CourseID_IsDeleted_idx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrolment_CourseID",
                table: "Enrolment");

            migrationBuilder.CreateIndex(
                name: "idxCourseID_IsDeleted",
                table: "Enrolment",
                columns: new[] { "CourseID", "IsDeleted" })
                .Annotation("SqlServer:Include", new[] { "ClassID", "Created", "CreatedOn", "DateEnrolled", "DeletedAt", "IsCourseClerk", "IsWaitlisted", "PersonID", "TermID", "UpdatedOn", "User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idxCourseID_IsDeleted",
                table: "Enrolment");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_CourseID",
                table: "Enrolment",
                column: "CourseID");
        }
    }
}
