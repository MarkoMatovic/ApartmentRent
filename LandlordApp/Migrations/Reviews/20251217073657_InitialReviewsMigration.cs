using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Reviews
{
    /// <inheritdoc />
    public partial class InitialReviewsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ReviewsFavorites");

            migrationBuilder.CreateTable(
                name: "Favorites",
                schema: "ReviewsFavorites",
                columns: table => new
                {
                    FavoriteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ApartmentId = table.Column<int>(type: "int", nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Favorite__CE74FAD5F5FEA175", x => x.FavoriteId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Favorites__UserI__6754599E",
                        column: x => x.UserId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    // Foreign key ka Listings.Apartments (cross-context)
                    table.ForeignKey(
                        name: "FK__Favorites__Apart__68487DD7",
                        column: x => x.ApartmentId,
                        principalSchema: "Listings",
                        principalTable: "Apartments",
                        principalColumn: "ApartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "ReviewsFavorites",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    LandlordId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", maxLength: 5, nullable: true),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Reviews__74BC79CE27F3C467", x => x.ReviewId);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Reviews__Landlor__6E01572D",
                        column: x => x.LandlordId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    // Foreign key ka UsersRoles.Users (cross-context)
                    table.ForeignKey(
                        name: "FK__Reviews__TenantI__6D0D32F4",
                        column: x => x.TenantId,
                        principalSchema: "UsersRoles",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_ApartmentId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                column: "ApartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_ApartmentId",
                schema: "ReviewsFavorites",
                table: "Favorites",
                columns: new[] { "UserId", "ApartmentId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [ApartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_LandlordId",
                schema: "ReviewsFavorites",
                table: "Reviews",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TenantId",
                schema: "ReviewsFavorites",
                table: "Reviews",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favorites",
                schema: "ReviewsFavorites");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "ReviewsFavorites");
        }
    }
}
