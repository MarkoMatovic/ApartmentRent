using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddRoommatePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Roommates_Search_Main",
                schema: "Roommates",
                table: "Roommates",
                columns: new[] { "PreferredLocation", "BudgetMin", "BudgetMax", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_Preferences",
                schema: "Roommates",
                table: "Roommates",
                columns: new[] { "SmokingAllowed", "PetFriendly", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_Lifestyle",
                schema: "Roommates",
                table: "Roommates",
                columns: new[] { "Lifestyle", "Cleanliness", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_Availability",
                schema: "Roommates",
                table: "Roommates",
                columns: new[] { "AvailableFrom", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_ApartmentLink",
                schema: "Roommates",
                table: "Roommates",
                columns: new[] { "LookingForApartmentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_Status",
                schema: "Roommates",
                table: "Roommates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roommates_Search_Main",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_Preferences",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_Lifestyle",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_Availability",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_ApartmentLink",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_Status",
                schema: "Roommates",
                table: "Roommates");
        }
    }
}
