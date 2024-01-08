using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Course_EditType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Course_CourseType_CourseTypeID",
                table: "Course");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseTypeID",
                table: "Course",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditType",
                table: "Course",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_CourseType_CourseTypeID",
                table: "Course",
                column: "CourseTypeID",
                principalTable: "CourseType",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Course_CourseType_CourseTypeID",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "EditType",
                table: "Course");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseTypeID",
                table: "Course",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Course_CourseType_CourseTypeID",
                table: "Course",
                column: "CourseTypeID",
                principalTable: "CourseType",
                principalColumn: "ID");
        }
    }
}
