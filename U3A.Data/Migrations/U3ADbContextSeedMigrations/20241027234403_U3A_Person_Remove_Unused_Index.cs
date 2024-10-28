using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_Person_Remove_Unused_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Person_ConversionID",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Person_DataImportTimestamp",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Person_LastName_FirstName_Email",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Person_PersonID",
                table: "Person");

            migrationBuilder.AlterColumn<string>(
                name: "DataImportTimestamp",
                table: "Person",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DataImportTimestamp",
                table: "Person",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_ConversionID",
                table: "Person",
                column: "ConversionID");

            migrationBuilder.CreateIndex(
                name: "IX_Person_DataImportTimestamp",
                table: "Person",
                column: "DataImportTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Person_LastName_FirstName_Email",
                table: "Person",
                columns: new[] { "LastName", "FirstName", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Person_PersonID",
                table: "Person",
                column: "PersonID");
        }
    }
}
