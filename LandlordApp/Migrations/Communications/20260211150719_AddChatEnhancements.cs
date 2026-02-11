using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Communications
{
    /// <inheritdoc />
    public partial class AddChatEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "Communication",
                table: "Messages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                schema: "Communication",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                schema: "Communication",
                table: "Messages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                schema: "Communication",
                table: "Messages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConversationSettings",
                schema: "Communication",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OtherUserId = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ConversationSettings__SettingId", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                schema: "Communication",
                columns: table => new
                {
                    EmailLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SendGridMessageId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EmailLogs__C87C0C9D", x => x.EmailLogId);
                });

            migrationBuilder.CreateTable(
                name: "ReportedMessages",
                schema: "Communication",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReportedUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getutcdate())"),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReportedMessages__ReportId", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK__ReportedMessages__Message",
                        column: x => x.MessageId,
                        principalSchema: "Communication",
                        principalTable: "Messages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSettings_UserId_OtherUserId",
                schema: "Communication",
                table: "ConversationSettings",
                columns: new[] { "UserId", "OtherUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_MessageId",
                schema: "Communication",
                table: "ReportedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_ReportedByUserId",
                schema: "Communication",
                table: "ReportedMessages",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_ReportedUserId",
                schema: "Communication",
                table: "ReportedMessages",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_Status",
                schema: "Communication",
                table: "ReportedMessages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationSettings",
                schema: "Communication");

            migrationBuilder.DropTable(
                name: "EmailLogs",
                schema: "Communication");

            migrationBuilder.DropTable(
                name: "ReportedMessages",
                schema: "Communication");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "Communication",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileSize",
                schema: "Communication",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileType",
                schema: "Communication",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                schema: "Communication",
                table: "Messages");

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Communication",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "Communication",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserRoleId = table.Column<int>(type: "int", nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_User_Role_UserRoleId",
                        column: x => x.UserRoleId,
                        principalSchema: "Communication",
                        principalTable: "Role",
                        principalColumn: "RoleId");
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

            migrationBuilder.CreateIndex(
                name: "IX_User_UserRoleId",
                schema: "Communication",
                table: "User",
                column: "UserRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK__Messages__Receiv__6383C8BA",
                schema: "Communication",
                table: "Messages",
                column: "ReceiverId",
                principalSchema: "Communication",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__Messages__Sender__628FA481",
                schema: "Communication",
                table: "Messages",
                column: "SenderId",
                principalSchema: "Communication",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
