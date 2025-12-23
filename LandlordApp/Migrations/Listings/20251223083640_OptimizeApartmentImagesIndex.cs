using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class OptimizeApartmentImagesIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Listings",
                table: "ApartmentImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentImages_ApartmentId_IsDeleted_IsPrimary",
                schema: "Listings",
                table: "ApartmentImages",
                columns: new[] { "ApartmentId", "IsDeleted", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Rent_City_Covering",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "Rent", "City" })
                .Annotation("SqlServer:Include", new[] 
                { 
                    "ApartmentId", "Title", "Address", "Latitude", "Longitude", 
                    "SizeSquareMeters", "ApartmentType", "IsFurnished", "IsImmediatelyAvailable"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApartmentImages_ApartmentId_IsDeleted_IsPrimary",
                schema: "Listings",
                table: "ApartmentImages");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Rent_City_Covering",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Listings",
                table: "ApartmentImages");
        }
    }
}
