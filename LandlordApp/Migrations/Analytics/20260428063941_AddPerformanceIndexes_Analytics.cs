using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Analytics
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes_Analytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId_EventType_CreatedDate",
                schema: "Analytics",
                table: "AnalyticsEvents",
                columns: new[] { "UserId", "EventType", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalyticsEvents_UserId_EventType_CreatedDate",
                schema: "Analytics",
                table: "AnalyticsEvents");
        }
    }
}
