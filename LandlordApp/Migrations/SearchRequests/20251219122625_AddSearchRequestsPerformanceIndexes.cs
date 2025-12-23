using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.SearchRequests
{
    /// <inheritdoc />
    public partial class AddSearchRequestsPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_Search_Main",
                schema: "SearchRequests",
                table: "SearchRequests",
                columns: new[] { "City", "RequestType", "IsActive", "BudgetMin", "BudgetMax" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_TypeLocation",
                schema: "SearchRequests",
                table: "SearchRequests",
                columns: new[] { "RequestType", "City", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_Budget",
                schema: "SearchRequests",
                table: "SearchRequests",
                columns: new[] { "BudgetMin", "BudgetMax", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_StatusDate",
                schema: "SearchRequests",
                table: "SearchRequests",
                columns: new[] { "IsActive", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_Preferences",
                schema: "SearchRequests",
                table: "SearchRequests",
                columns: new[] { "PetFriendly", "SmokingAllowed", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_Search_Main",
                schema: "SearchRequests",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_TypeLocation",
                schema: "SearchRequests",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_Budget",
                schema: "SearchRequests",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_StatusDate",
                schema: "SearchRequests",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_Preferences",
                schema: "SearchRequests",
                table: "SearchRequests");
        }
    }
}
