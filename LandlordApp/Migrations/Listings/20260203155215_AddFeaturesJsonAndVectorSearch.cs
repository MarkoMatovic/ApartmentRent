using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddFeaturesJsonAndVectorSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasAirCondition",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasBalcony",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasElevator",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasInternet",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasParking",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsFurnished",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsPetFriendly",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsSmokingAllowed",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEmbedding",
                schema: "Listings",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                schema: "Listings",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments",
                column: "ApartmentType");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments",
                column: "IsImmediatelyAvailable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "DescriptionEmbedding",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Features",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAirCondition",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBalcony",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasElevator",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasInternet",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasParking",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFurnished",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPetFriendly",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmokingAllowed",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "IsActive", "IsDeleted" });
        }
    }
}
