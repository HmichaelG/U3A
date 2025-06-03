using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_LuckyMemberDrawEntrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "LuckyMemberDraw",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "LuckyMemberDrawEntrant",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LuckyMemberDrawID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyMemberDrawEntrant", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LuckyMemberDrawEntrant_LuckyMemberDraw_LuckyMemberDrawID",
                        column: x => x.LuckyMemberDrawID,
                        principalTable: "LuckyMemberDraw",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LuckyMemberDrawEntrant_Person_PersonID",
                        column: x => x.PersonID,
                        principalTable: "Person",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LuckyMemberDrawEntrant_LuckyMemberDrawID",
                table: "LuckyMemberDrawEntrant",
                column: "LuckyMemberDrawID");

            migrationBuilder.CreateIndex(
                name: "IX_LuckyMemberDrawEntrant_PersonID",
                table: "LuckyMemberDrawEntrant",
                column: "PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LuckyMemberDrawEntrant");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LuckyMemberDraw");
        }
    }
}
