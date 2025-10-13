using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_RowVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Volunteer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Volunteer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Volunteer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Volunteer",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Venue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Venue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Venue",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Venue",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Term",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Term",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Term",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Term",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Tag",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Tag",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Tag",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Tag",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "SystemSettings",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "SendMail",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Schedule",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Report",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Report",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Report",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Report",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "ReceiptDataImport",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "ReceiptDataImport",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "ReceiptDataImport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "ReceiptDataImport",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Receipt",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PublicHoliday",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "PublicHoliday",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "PublicHoliday",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "PublicHoliday",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PersonImportError",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "PersonImportError",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "PersonImportError",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "PersonImportError",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PersonImport",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "PersonImport",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "PersonImport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "PersonImport",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Person",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "OnlinePaymentStatus",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Leave",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Fee",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Enrolment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Dropout",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Dropout",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Dropout",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Dropout",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "DocumentType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "DocumentType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "DocumentType",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "DocumentType",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "DocumentTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "DocumentTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "DocumentTemplate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "DocumentTemplate",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "DocumentQueue",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "CourseType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "CourseType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "CourseType",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "CourseType",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Course",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Committee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Committee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "Committee",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Committee",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Class",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "CancelClass",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "CancelClass",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "CancelClass",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "CancelClass",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "AttendClass",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "AttendClass",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "AttendClass",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "AttendClass",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.UpdateData(
                table: "DocumentType",
                keyColumn: "ID",
                keyValue: 0,
                columns: new[] { "CreatedOn", "UpdatedOn", "User" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DocumentType",
                keyColumn: "ID",
                keyValue: 1,
                columns: new[] { "CreatedOn", "UpdatedOn", "User" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DocumentType",
                keyColumn: "ID",
                keyValue: 2,
                columns: new[] { "CreatedOn", "UpdatedOn", "User" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DocumentType",
                keyColumn: "ID",
                keyValue: 3,
                columns: new[] { "CreatedOn", "UpdatedOn", "User" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DocumentType",
                keyColumn: "ID",
                keyValue: 4,
                columns: new[] { "CreatedOn", "UpdatedOn", "User" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Volunteer");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Volunteer");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Volunteer");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Volunteer");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Term");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Term");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Term");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Term");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "User",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SendMail");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "ReceiptDataImport");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "ReceiptDataImport");

            migrationBuilder.DropColumn(
                name: "User",
                table: "ReceiptDataImport");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReceiptDataImport");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PublicHoliday");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "PublicHoliday");

            migrationBuilder.DropColumn(
                name: "User",
                table: "PublicHoliday");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PublicHoliday");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PersonImportError");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "PersonImportError");

            migrationBuilder.DropColumn(
                name: "User",
                table: "PersonImportError");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PersonImportError");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PersonImport");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "PersonImport");

            migrationBuilder.DropColumn(
                name: "User",
                table: "PersonImport");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PersonImport");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OnlinePaymentStatus");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Leave");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Fee");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Enrolment");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Dropout");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Dropout");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Dropout");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Dropout");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "DocumentType");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "DocumentType");

            migrationBuilder.DropColumn(
                name: "User",
                table: "DocumentType");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DocumentType");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "DocumentTemplate");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "DocumentTemplate");

            migrationBuilder.DropColumn(
                name: "User",
                table: "DocumentTemplate");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DocumentTemplate");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DocumentQueue");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "CourseType");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "CourseType");

            migrationBuilder.DropColumn(
                name: "User",
                table: "CourseType");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CourseType");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Committee");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Committee");

            migrationBuilder.DropColumn(
                name: "User",
                table: "Committee");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Committee");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Class");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "CancelClass");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "CancelClass");

            migrationBuilder.DropColumn(
                name: "User",
                table: "CancelClass");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CancelClass");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "AttendClass");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "AttendClass");

            migrationBuilder.DropColumn(
                name: "User",
                table: "AttendClass");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AttendClass");
        }
    }
}
