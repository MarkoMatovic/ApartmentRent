using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Reviews
{
    /// <inheritdoc />
    public partial class AddReviewApartmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ApartmentId",
                schema: "ReviewsFavorites",
                table: "Reviews",
                column: "ApartmentId")
                .Annotation("SqlServer:Include", new[] { "Rating", "IsPublic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ApartmentId",
                schema: "ReviewsFavorites",
                table: "Reviews");
        }
    }
}
