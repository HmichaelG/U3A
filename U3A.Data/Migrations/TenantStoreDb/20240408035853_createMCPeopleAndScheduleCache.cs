using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class createMCPeopleAndScheduleCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiCampusPerson");

            migrationBuilder.DropTable(
                name: "Schedule");

            migrationBuilder.CreateTable(
                name: "MultiCampusPeople",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostNominals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HomePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SMSOptOut = table.Column<bool>(type: "bit", nullable: false),
                    ICEContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ICEPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaxCertificateViewed = table.Column<bool>(type: "bit", nullable: false),
                    Communication = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCampusPeople", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleCache",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    jsonClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    jsonClassEnrolments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    jsonCourseEnrolments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleCache", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiCampusPeople_LastName_FirstName_Email",
                table: "MultiCampusPeople",
                columns: new[] { "LastName", "FirstName", "Email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiCampusPeople");

            migrationBuilder.DropTable(
                name: "ScheduleCache");

            migrationBuilder.CreateTable(
                name: "MultiCampusPerson",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Communication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HomePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ICEContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ICEPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostNominals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SMSOptOut = table.Column<bool>(type: "bit", nullable: false),
                    TenantIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VaxCertificateViewed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCampusPerson", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    jsonClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    jsonClassEnrolments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    jsonCourseEnrolments = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiCampusPerson_LastName_FirstName_Email",
                table: "MultiCampusPerson",
                columns: new[] { "LastName", "FirstName", "Email" });
        }
    }
}
