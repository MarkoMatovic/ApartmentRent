using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Reviews
{
    /// <inheritdoc />
    public partial class AddRoommateAndSearchRequestToFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                column: "RoommateId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                column: "SearchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                columns: new[] { "UserId", "RoommateId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [RoommateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                columns: new[] { "UserId", "SearchRequestId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [SearchRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "RoommateId",
                schema: "ReviewsFavorites",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "SearchRequestId",
                schema: "ReviewsFavorites",
                table: "Favorites");
        }
    }
}
