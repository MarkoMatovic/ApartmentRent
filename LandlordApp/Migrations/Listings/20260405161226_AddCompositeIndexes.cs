using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsActive_IsDeleted_CreatedDate",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "IsActive", "IsDeleted", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted_City",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsActive_IsDeleted_CreatedDate",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
