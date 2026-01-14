using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class UpdateExistingApartmentsLandlordId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing apartments to set LandlordId based on CreatedByGuid
            migrationBuilder.Sql(@"
                UPDATE a
                SET a.LandlordId = u.UserId
                FROM [Listings].[Apartments] a
                INNER JOIN [UsersRoles].[Users] u ON a.CreatedByGuid = u.UserGuid
                WHERE a.LandlordId IS NULL AND a.CreatedByGuid IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
