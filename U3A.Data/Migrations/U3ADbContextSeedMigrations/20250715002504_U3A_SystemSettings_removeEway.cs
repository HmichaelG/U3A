using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_SystemSettings_removeEway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantFeeFixed",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MerchantFeePercentage",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SeparateMerchantFeeAndU3AFee",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MerchantFee",
                table: "Receipt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MerchantFeeFixed",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MerchantFeePercentage",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SeparateMerchantFeeAndU3AFee",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MerchantFee",
                table: "Receipt",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
