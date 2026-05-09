using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddListingExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ListingExpiresAt",
                schema: "Listings",
                table: "Apartments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                schema: "Listings",
                table: "Apartments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ListingExpiresAt",
                schema: "Listings",
                table: "Apartments",
                column: "ListingExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_ListingExpiresAt",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "ListingExpiresAt",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
