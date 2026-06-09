using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddPremiumFeatureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListingCredits",
                schema: "UsersRoles",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriorityInboxUntil",
                schema: "UsersRoles",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListingCredits",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PriorityInboxUntil",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
