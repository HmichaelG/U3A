using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class TdB_Term_delCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MultiCampusTerm",
                table: "MultiCampusTerm");

            migrationBuilder.AlterColumn<string>(
                name: "TenantIdentifier",
                table: "MultiCampusTerm",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MultiCampusTerm",
                table: "MultiCampusTerm",
                column: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MultiCampusTerm",
                table: "MultiCampusTerm");

            migrationBuilder.AlterColumn<string>(
                name: "TenantIdentifier",
                table: "MultiCampusTerm",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MultiCampusTerm",
                table: "MultiCampusTerm",
                columns: new[] { "ID", "TenantIdentifier" });
        }
    }
}
