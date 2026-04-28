using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Notification
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_IsRead_CreatedDate",
                schema: "Notification",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "IsRead", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_IsRead_CreatedDate",
                schema: "Notification",
                table: "Notifications");
        }
    }
}
