using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_DocumentQueueAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "DocumentQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "DocumentQueue",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "DocumentQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "User",
                table: "DocumentQueue",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentQueueAttachment",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attachment = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    DocumentQueueID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentQueueAttachment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DocumentQueueAttachment_DocumentQueue_DocumentQueueID",
                        column: x => x.DocumentQueueID,
                        principalTable: "DocumentQueue",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentQueueAttachment_DocumentQueueID",
                table: "DocumentQueueAttachment",
                column: "DocumentQueueID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentQueueAttachment");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "DocumentQueue");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "DocumentQueue");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "DocumentQueue");

            migrationBuilder.DropColumn(
                name: "User",
                table: "DocumentQueue");
        }
    }
}
