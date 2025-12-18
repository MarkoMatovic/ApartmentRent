using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class InitialRoommatesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Roommates");

            migrationBuilder.CreateTable(
                name: "Roommates",
                schema: "Roommates",
                columns: table => new
                {
                    RoommateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    Hobbies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Profession = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmokingAllowed = table.Column<bool>(type: "bit", nullable: true),
                    PetFriendly = table.Column<bool>(type: "bit", nullable: true),
                    Lifestyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Cleanliness = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GuestsAllowed = table.Column<bool>(type: "bit", nullable: true),
                    BudgetMin = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    BudgetMax = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    BudgetIncludes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    AvailableUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    MinimumStayMonths = table.Column<int>(type: "int", nullable: true),
                    MaximumStayMonths = table.Column<int>(type: "int", nullable: true),
                    LookingForRoomType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LookingForApartmentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreferredLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roommates__RoommateId", x => x.RoommateId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Roommates__User__UserId",
                        column: x => x.UserId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_UserId",
                schema: "Roommates",
                table: "Roommates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Roommates",
                schema: "Roommates");
        }
    }
}
