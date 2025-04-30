using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class AppUser_TwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorNotRequired",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TwoFactorGracePeriodStart",
                table: "AspNetUsers",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwoFactorNotRequired",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorGracePeriodStart",
                table: "AspNetUsers");
        }
    }
}
