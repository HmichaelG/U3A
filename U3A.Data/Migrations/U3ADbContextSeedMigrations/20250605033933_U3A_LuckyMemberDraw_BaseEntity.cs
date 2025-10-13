using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_LuckyMemberDraw_BaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "LuckyMemberDraw",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "LuckyMemberDraw",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "LuckyMemberDraw",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "LuckyMemberDraw");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "LuckyMemberDraw");

            migrationBuilder.DropColumn(
                name: "User",
                table: "LuckyMemberDraw");
        }
    }
}
