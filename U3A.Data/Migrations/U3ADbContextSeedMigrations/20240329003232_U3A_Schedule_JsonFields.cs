using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Schedule_JsonFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassOccurenceID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassOfferedT1",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassOfferedT2",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassOfferedT3",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassOfferedT4",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassRecurrence",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassStartDate",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassSummary",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClassTime",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClerkEmail",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClerkMobile",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClerkName",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ClerkPhone",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseCost",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseCostDescription",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseCostTermDescription",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseDescription",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseMaximum",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseName",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseNumber",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseParticipationTypeID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseRequired",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseTermCost",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CourseType",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "GuestLeader",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "IsOffScheduleActivity",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LeaderEmail",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LeaderMobile",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LeaderName",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LeaderPhone",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "OnDayID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "TermNumber",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "VenueCanMapAddress",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Schedule");

            migrationBuilder.RenameColumn(
                name: "VenueName",
                table: "Schedule",
                newName: "jsonCourseEnrolments");

            migrationBuilder.RenameColumn(
                name: "VenueAddress",
                table: "Schedule",
                newName: "jsonClassEnrolments");

            migrationBuilder.RenameColumn(
                name: "TermSummary",
                table: "Schedule",
                newName: "jsonClass");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "jsonCourseEnrolments",
                table: "Schedule",
                newName: "VenueName");

            migrationBuilder.RenameColumn(
                name: "jsonClassEnrolments",
                table: "Schedule",
                newName: "VenueAddress");

            migrationBuilder.RenameColumn(
                name: "jsonClass",
                table: "Schedule",
                newName: "TermSummary");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassID",
                table: "Schedule",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ClassOccurenceID",
                table: "Schedule",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClassOfferedT1",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClassOfferedT2",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClassOfferedT3",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClassOfferedT4",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClassRecurrence",
                table: "Schedule",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassStartDate",
                table: "Schedule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassSummary",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassTime",
                table: "Schedule",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ClerkEmail",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClerkMobile",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClerkName",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClerkPhone",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CourseCost",
                table: "Schedule",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CourseCostDescription",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseCostTermDescription",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseDescription",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CourseID",
                table: "Schedule",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "CourseMaximum",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CourseName",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CourseNumber",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CourseParticipationTypeID",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CourseRequired",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CourseTermCost",
                table: "Schedule",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CourseType",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuestLeader",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOffScheduleActivity",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LeaderEmail",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeaderMobile",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeaderName",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeaderPhone",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OnDayID",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TermNumber",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "VenueCanMapAddress",
                table: "Schedule",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
