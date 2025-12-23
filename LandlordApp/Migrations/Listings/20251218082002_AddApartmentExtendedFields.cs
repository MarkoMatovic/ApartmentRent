using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddApartmentExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApartmentType",
                schema: "Listings",
                table: "Apartments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(10,2)",
                nullable: true);

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
                name: "IsImmediatelyAvailable",
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

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumStayMonths",
                schema: "Listings",
                table: "Apartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStayMonths",
                schema: "Listings",
                table: "Apartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizeSquareMeters",
                schema: "Listings",
                table: "Apartments",
                type: "int",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_Latitude_Longitude",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "Latitude", "Longitude" });
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

            migrationBuilder.DropIndex(
                name: "IX_Apartments_Latitude_Longitude",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "ApartmentType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
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
                name: "IsImmediatelyAvailable",
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

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "MaximumStayMonths",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "MinimumStayMonths",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "SizeSquareMeters",
                schema: "Listings",
                table: "Apartments");
        }
    }
}
