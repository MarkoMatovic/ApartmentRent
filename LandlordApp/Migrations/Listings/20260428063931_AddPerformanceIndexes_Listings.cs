using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes_Listings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsFeatured_IsActive_IsDeleted_CreatedDate",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsFeatured", "IsActive", "IsDeleted", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsFeatured_IsActive_IsDeleted_CreatedDate",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
