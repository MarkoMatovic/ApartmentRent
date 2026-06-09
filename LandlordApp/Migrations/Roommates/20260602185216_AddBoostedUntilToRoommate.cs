using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddBoostedUntilToRoommate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BoostedUntil",
                schema: "Roommates",
                table: "Roommates",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoostedUntil",
                schema: "Roommates",
                table: "Roommates");
        }
    }
}
