using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToApartmentImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "Listings",
                table: "ApartmentImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Set DisplayOrder for existing images based on ImageId
            // This ensures existing images have a valid order
            migrationBuilder.Sql(@"
                WITH OrderedImages AS (
                    SELECT ImageId, 
                           ROW_NUMBER() OVER (PARTITION BY ApartmentId ORDER BY ImageId) - 1 AS DisplayOrder
                    FROM Listings.ApartmentImages
                    WHERE IsDeleted = 0
                )
                UPDATE ai
                SET ai.DisplayOrder = oi.DisplayOrder
                FROM Listings.ApartmentImages ai
                INNER JOIN OrderedImages oi ON ai.ImageId = oi.ImageId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "Listings",
                table: "ApartmentImages");
        }
    }
}
