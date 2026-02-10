using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddOptimizedIndexes_Phase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City",
                schema: "Listings",
                table: "Apartments",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_CreatedDate",
                schema: "Listings",
                table: "Apartments",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ListingType",
                schema: "Listings",
                table: "Apartments",
                column: "ListingType");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_NumberOfRooms",
                schema: "Listings",
                table: "Apartments",
                column: "NumberOfRooms");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Price",
                schema: "Listings",
                table: "Apartments",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Rent",
                schema: "Listings",
                table: "Apartments",
                column: "Rent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_CreatedDate",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_ListingType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_NumberOfRooms",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Price",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Rent",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
