using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class TdB_MCPerson_Address : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "MultiCampusPerson",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "MultiCampusPerson",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Postcode",
                table: "MultiCampusPerson",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "MultiCampusPerson",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "City",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "MultiCampusPerson");

            migrationBuilder.DropColumn(
                name: "State",
                table: "MultiCampusPerson");
        }
    }
}
