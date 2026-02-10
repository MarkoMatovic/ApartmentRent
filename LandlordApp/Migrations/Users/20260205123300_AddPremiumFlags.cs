using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddPremiumFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasLandlordAnalytics",
                schema: "UsersRoles",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPersonalAnalytics",
                schema: "UsersRoles",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasLandlordAnalytics",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HasPersonalAnalytics",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
