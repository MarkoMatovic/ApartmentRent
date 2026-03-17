using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Communications
{
    /// <inheritdoc />
    public partial class RenameEmailLogSendGridToProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SendGridMessageId",
                schema: "Communication",
                table: "EmailLogs",
                newName: "ProviderMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderMessageId",
                schema: "Communication",
                table: "EmailLogs",
                newName: "SendGridMessageId");
        }
    }
}
