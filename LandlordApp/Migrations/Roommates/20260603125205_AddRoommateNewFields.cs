using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddRoommateNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                schema: "Roommates",
                table: "Roommates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                schema: "Roommates",
                table: "Roommates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MusicFriendly",
                schema: "Roommates",
                table: "Roommates",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkSchedule",
                schema: "Roommates",
                table: "Roommates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropColumn(
                name: "Languages",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropColumn(
                name: "MusicFriendly",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropColumn(
                name: "WorkSchedule",
                schema: "Roommates",
                table: "Roommates");
        }
    }
}
