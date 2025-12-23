using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddIsLookingForRoommateToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLookingForRoommate",
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
                name: "IsLookingForRoommate",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
