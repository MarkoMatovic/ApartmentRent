using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations
{
    /// <inheritdoc />
    public partial class InitialApplicationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Applications");

            migrationBuilder.CreateTable(
                name: "ApartmentApplications",
                schema: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ApartmentId = table.Column<int>(type: "int", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ApartmentApplications__C93A4C99A9D487DE", x => x.ApplicationId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__ApartmentApplications__UserId",
                        column: x => x.UserId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    // Foreign key ka Listings.Apartments (cross-context)
                    table.ForeignKey(
                        name: "FK__ApartmentApplications__ApartmentId",
                        column: x => x.ApartmentId,
                        principalSchema: "Listings",
                        principalTable: "Apartments",
                        principalColumn: "ApartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchPreferences",
                schema: "Applications",
                columns: table => new
                {
                    PreferenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    MaxRent = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MinRooms = table.Column<int>(type: "int", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    AvailableUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SearchPr__E228496FC715AE79", x => x.PreferenceId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__SearchPre__UserI__787EE5A0",
                        column: x => x.UserId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentApplications_ApartmentId",
                schema: "Applications",
                table: "ApartmentApplications",
                column: "ApartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentApplications_UserId",
                schema: "Applications",
                table: "ApartmentApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchPreferences_UserId",
                schema: "Applications",
                table: "SearchPreferences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApartmentApplications",
                schema: "Applications");

            migrationBuilder.DropTable(
                name: "SearchPreferences",
                schema: "Applications");
        }
    }
}
