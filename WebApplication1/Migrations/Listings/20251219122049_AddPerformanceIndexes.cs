using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Search_Main",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "IsActive", "IsDeleted", "Rent" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Filters_Boolean",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsPetFriendly", "IsSmokingAllowed", "IsFurnished", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_RoomType",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "NumberOfRooms", "ApartmentType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Availability",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsImmediatelyAvailable", "AvailableFrom", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Status",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_Search_Main",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Filters_Boolean",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_RoomType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Availability",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Status",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
