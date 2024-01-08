using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_AttendClassCascadeRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendClass_Class_ClassID",
                table: "AttendClass");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassID",
                table: "AttendClass",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendClass_Class_ClassID",
                table: "AttendClass",
                column: "ClassID",
                principalTable: "Class",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendClass_Class_ClassID",
                table: "AttendClass");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassID",
                table: "AttendClass",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendClass_Class_ClassID",
                table: "AttendClass",
                column: "ClassID",
                principalTable: "Class",
                principalColumn: "ID");
        }
    }
}
