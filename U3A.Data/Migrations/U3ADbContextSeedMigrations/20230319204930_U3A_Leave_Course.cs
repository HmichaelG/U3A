using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Leave_Course : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseID",
                table: "Leave",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leave_CourseID",
                table: "Leave",
                column: "CourseID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leave_Course_CourseID",
                table: "Leave",
                column: "CourseID",
                principalTable: "Course",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leave_Course_CourseID",
                table: "Leave");

            migrationBuilder.DropIndex(
                name: "IX_Leave_CourseID",
                table: "Leave");

            migrationBuilder.DropColumn(
                name: "CourseID",
                table: "Leave");
        }
    }
}
