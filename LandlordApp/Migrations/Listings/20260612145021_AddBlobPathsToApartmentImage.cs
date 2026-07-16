using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddBlobPathsToApartmentImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobPath",
                schema: "Listings",
                table: "ApartmentImages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                schema: "Listings",
                table: "ApartmentImages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlobPath",
                schema: "Listings",
                table: "ApartmentImages");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                schema: "Listings",
                table: "ApartmentImages");
        }
    }
}
