using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddPrivacySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnalyticsConsent",
                schema: "UsersRoles",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ChatHistoryConsent",
                schema: "UsersRoles",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileVisibility",
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
                name: "AnalyticsConsent",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatHistoryConsent",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
