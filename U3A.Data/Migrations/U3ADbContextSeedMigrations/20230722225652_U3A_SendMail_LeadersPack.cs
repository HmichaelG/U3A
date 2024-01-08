using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_SendMail_LeadersPack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrintAttendanceRecord",
                table: "SendMail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintCSVFile",
                table: "SendMail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintClassList",
                table: "SendMail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintICEList",
                table: "SendMail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintLeaderReport",
                table: "SendMail",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintAttendanceRecord",
                table: "SendMail");

            migrationBuilder.DropColumn(
                name: "PrintCSVFile",
                table: "SendMail");

            migrationBuilder.DropColumn(
                name: "PrintClassList",
                table: "SendMail");

            migrationBuilder.DropColumn(
                name: "PrintICEList",
                table: "SendMail");

            migrationBuilder.DropColumn(
                name: "PrintLeaderReport",
                table: "SendMail");
        }
    }
}
