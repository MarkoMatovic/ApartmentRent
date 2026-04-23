using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Applications
{
    /// <inheritdoc />
    public partial class AddUniqueApplicationConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApartmentApplications_UserId_ApartmentId",
                schema: "Applications",
                table: "ApartmentApplications",
                columns: new[] { "UserId", "ApartmentId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [ApartmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApartmentApplications_UserId_ApartmentId",
                schema: "Applications",
                table: "ApartmentApplications");
        }
    }
}
