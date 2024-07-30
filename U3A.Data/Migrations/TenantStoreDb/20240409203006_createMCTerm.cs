using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class createMCTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiCampusPeople");

            migrationBuilder.CreateTable(
                name: "MultiCampusPerson",
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
                    table.PrimaryKey("PK_MultiCampusPerson", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MultiCampusTerm",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TermNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    EnrolmentStarts = table.Column<int>(type: "int", nullable: false),
                    EnrolmentEnds = table.Column<int>(type: "int", nullable: false),
                    IsDefaultTerm = table.Column<bool>(type: "bit", nullable: false),
                    IsClassAllocationFinalised = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCampusTerm", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiCampusPerson_LastName_FirstName_Email",
                table: "MultiCampusPerson",
                columns: new[] { "LastName", "FirstName", "Email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiCampusPerson");

            migrationBuilder.DropTable(
                name: "MultiCampusTerm");

            migrationBuilder.CreateTable(
                name: "MultiCampusPeople",
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
                    table.PrimaryKey("PK_MultiCampusPeople", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiCampusPeople_LastName_FirstName_Email",
                table: "MultiCampusPeople",
                columns: new[] { "LastName", "FirstName", "Email" });
        }
    }
}
