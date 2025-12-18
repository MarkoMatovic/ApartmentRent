using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.SearchRequests
{
    /// <inheritdoc />
    public partial class InitialSearchRequestsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SearchRequests");

            migrationBuilder.CreateTable(
                name: "SearchRequests",
                schema: "SearchRequests",
                columns: table => new
                {
                    SearchRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PreferredLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BudgetMin = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    BudgetMax = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    NumberOfRooms = table.Column<int>(type: "int", nullable: true),
                    SizeSquareMeters = table.Column<int>(type: "int", nullable: true),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: true),
                    HasParking = table.Column<bool>(type: "bit", nullable: true),
                    HasBalcony = table.Column<bool>(type: "bit", nullable: true),
                    PetFriendly = table.Column<bool>(type: "bit", nullable: true),
                    SmokingAllowed = table.Column<bool>(type: "bit", nullable: true),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    AvailableUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    LookingForSmokingAllowed = table.Column<bool>(type: "bit", nullable: true),
                    LookingForPetFriendly = table.Column<bool>(type: "bit", nullable: true),
                    PreferredLifestyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SearchRequests__SearchRequestId", x => x.SearchRequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_UserId",
                schema: "SearchRequests",
                table: "SearchRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchRequests",
                schema: "SearchRequests");
        }
    }
}
