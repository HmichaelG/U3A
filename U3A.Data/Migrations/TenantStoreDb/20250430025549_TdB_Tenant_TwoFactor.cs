using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    public partial class TdB_Tenant_TwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorNotRequired",
                table: "TenantInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwoFactorNotRequired",
                table: "TenantInfo");
        }
    }
}
