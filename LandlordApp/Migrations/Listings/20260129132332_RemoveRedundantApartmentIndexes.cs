using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class RemoveRedundantApartmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_City_NumberOfRooms",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_City_Rent",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_CreatedDate",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsDeleted",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_ListingType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_ListingType_City_Rent",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_NumberOfRooms",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Rent",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_SizeSquareMeters",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "IsActive", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments",
                column: "ApartmentType");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City",
                schema: "Listings",
                table: "Apartments",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City_NumberOfRooms",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "NumberOfRooms" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City_Rent",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "Rent" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_CreatedDate",
                schema: "Listings",
                table: "Apartments",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive",
                schema: "Listings",
                table: "Apartments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsDeleted",
                schema: "Listings",
                table: "Apartments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments",
                column: "IsImmediatelyAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ListingType",
                schema: "Listings",
                table: "Apartments",
                column: "ListingType");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ListingType_City_Rent",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "ListingType", "City", "Rent" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_NumberOfRooms",
                schema: "Listings",
                table: "Apartments",
                column: "NumberOfRooms");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Rent",
                schema: "Listings",
                table: "Apartments",
                column: "Rent");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_SizeSquareMeters",
                schema: "Listings",
                table: "Apartments",
                column: "SizeSquareMeters");
        }
    }
}
