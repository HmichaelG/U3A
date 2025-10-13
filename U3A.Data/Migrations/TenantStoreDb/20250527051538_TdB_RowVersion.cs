using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class TdB_RowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "MultiCampusTerm",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "MultiCampusTerm",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "MultiCampusTerm",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "MultiCampusTerm",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "MultiCampusSendMail",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "MultiCampusSchedule",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "MultiCampusPerson",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "MultiCampusPerson",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "MultiCampusPerson",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "MultiCampusPerson",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "MultiCampusEnrolment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "MultiCampusEnrolment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "MultiCampusEnrolment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "MultiCampusEnrolment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "ContactRequest",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "MultiCampusTerm");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "MultiCampusTerm");

            migrationBuilder.DropColumn(
                name: "User",
                table: "MultiCampusTerm");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MultiCampusTerm");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MultiCampusSendMail");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MultiCampusSchedule");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "User",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "MultiCampusEnrolment");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "MultiCampusEnrolment");

            migrationBuilder.DropColumn(
                name: "User",
                table: "MultiCampusEnrolment");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MultiCampusEnrolment");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ContactRequest");
        }
    }
}
