using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Reviews
{
    /// <inheritdoc />
    public partial class AddReviewApartmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApartmentId",
                schema: "ReviewsFavorites",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                schema: "ReviewsFavorites",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                schema: "ReviewsFavorites",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApartmentId",
                schema: "ReviewsFavorites",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                schema: "ReviewsFavorites",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                schema: "ReviewsFavorites",
                table: "Reviews");
        }
    }
}
