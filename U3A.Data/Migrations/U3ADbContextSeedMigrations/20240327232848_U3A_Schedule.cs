using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Schedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnDayID = table.Column<int>(type: "int", nullable: false),
                    CourseNumber = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseMaximum = table.Column<int>(type: "int", nullable: false),
                    CourseMinimu = table.Column<int>(type: "int", nullable: false),
                    CourseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CourseCostDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseTermCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CourseCostTermDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TermSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VenueAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuestLeader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderMobile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedule");
        }
    }
}
