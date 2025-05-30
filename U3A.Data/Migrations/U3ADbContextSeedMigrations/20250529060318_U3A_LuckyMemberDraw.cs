using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_LuckyMemberDraw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LuckyMemberDraw",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PersonID = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyMemberDraw", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LuckyMemberDraw");
        }
    }
}
