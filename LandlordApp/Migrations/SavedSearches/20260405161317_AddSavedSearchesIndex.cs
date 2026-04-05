using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.SavedSearches
{
    /// <inheritdoc />
    public partial class AddSavedSearchesIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_IsActive_EmailNotifications",
                schema: "SavedSearches",
                table: "SavedSearches",
                columns: new[] { "IsActive", "EmailNotificationsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedSearches_IsActive_EmailNotifications",
                schema: "SavedSearches",
                table: "SavedSearches");
        }
    }
}
