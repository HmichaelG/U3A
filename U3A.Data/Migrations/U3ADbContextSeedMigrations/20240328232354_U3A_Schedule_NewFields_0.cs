using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Schedule_NewFields_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LeaderType",
                table: "Schedule",
                newName: "ClerkPhone");

            migrationBuilder.RenameColumn(
                name: "CourseMinimu",
                table: "Schedule",
                newName: "CourseRequired");

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

            migrationBuilder.AddColumn<int>(
                name: "CourseParticipationTypeID",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "CourseParticipationTypeID",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "VenueCanMapAddress",
                table: "Schedule");

            migrationBuilder.RenameColumn(
                name: "CourseRequired",
                table: "Schedule",
                newName: "CourseMinimu");

            migrationBuilder.RenameColumn(
                name: "ClerkPhone",
                table: "Schedule",
                newName: "LeaderType");
        }
    }
}
