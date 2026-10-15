using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddRoommatePairing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pairings",
                schema: "Roommates",
                columns: table => new
                {
                    PairingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterUserId = table.Column<int>(type: "int", nullable: false),
                    PartnerUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    RespondedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pairings", x => x.PairingId);
                });

            migrationBuilder.CreateTable(
                name: "PairingApartments",
                schema: "Roommates",
                columns: table => new
                {
                    PairingApartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PairingId = table.Column<int>(type: "int", nullable: false),
                    ApartmentId = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddedDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PairingApartments", x => x.PairingApartmentId);
                    table.ForeignKey(
                        name: "FK_PairingApartments_Pairings_PairingId",
                        column: x => x.PairingId,
                        principalSchema: "Roommates",
                        principalTable: "Pairings",
                        principalColumn: "PairingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PairingApartments_PairingId_ApartmentId",
                schema: "Roommates",
                table: "PairingApartments",
                columns: new[] { "PairingId", "ApartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pairings_PartnerUserId_Status",
                schema: "Roommates",
                table: "Pairings",
                columns: new[] { "PartnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Pairings_RequesterUserId_Status",
                schema: "Roommates",
                table: "Pairings",
                columns: new[] { "RequesterUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PairingApartments",
                schema: "Roommates");

            migrationBuilder.DropTable(
                name: "Pairings",
                schema: "Roommates");
        }
    }
}
