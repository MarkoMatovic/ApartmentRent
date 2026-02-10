using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddRoommatesIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for user's roommate profile
            /*
            migrationBuilder.CreateIndex(
                name: "IX_Roommates_UserId",
                table: "Roommates",
                schema: "Roommates",
                column: "UserId",
                unique: true);
            */

            // Index for active roommate searches
            migrationBuilder.CreateIndex(
                name: "IX_Roommates_IsActive",
                table: "Roommates",
                schema: "Roommates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roommates_UserId",
                table: "Roommates",
                schema: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_IsActive",
                table: "Roommates",
                schema: "Roommates");
        }
    }
}
