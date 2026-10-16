using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddListingSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "City" })
                .Annotation("SqlServer:Include", new[] { "Rent", "NumberOfRooms", "ApartmentType", "ListingType", "IsImmediatelyAvailable", "AvailableFrom", "IsFeatured", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_List",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "IsFeatured", "CreatedDate" },
                descending: new[] { false, false, true, true })
                .Annotation("SqlServer:Include", new[] { "City", "Rent", "NumberOfRooms", "ApartmentType", "ListingType", "IsImmediatelyAvailable", "AvailableFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_List",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "City" });
        }
    }
}
