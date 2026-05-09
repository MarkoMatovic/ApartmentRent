using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Communications
{
    /// <inheritdoc />
    public partial class AddEmailLogSentAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentAt",
                schema: "Communication",
                table: "EmailLogs",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_SentAt",
                schema: "Communication",
                table: "EmailLogs");
        }
    }
}
