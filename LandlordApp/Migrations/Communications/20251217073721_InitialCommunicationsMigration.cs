using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Communications
{
    /// <inheritdoc />
    public partial class InitialCommunicationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Communication");

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "Communication",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    ReceiverId = table.Column<int>(type: "int", nullable: true),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Messages__C87C0C9CEDE67554", x => x.MessageId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Messages__Receiv__6383C8BA",
                        column: x => x.ReceiverId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Messages__Sender__628FA481",
                        column: x => x.SenderId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                schema: "Communication",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                schema: "Communication",
                table: "Messages",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages",
                schema: "Communication");
        }
    }
}
