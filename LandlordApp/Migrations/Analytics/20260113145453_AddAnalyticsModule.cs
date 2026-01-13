using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Analytics
{
    /// <inheritdoc />
    public partial class AddAnalyticsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Analytics");

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                schema: "Analytics",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SearchQuery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AnalyticsEvents__EventId", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_CreatedDate",
                schema: "Analytics",
                table: "AnalyticsEvents",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EntityId",
                schema: "Analytics",
                table: "AnalyticsEvents",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventCategory",
                schema: "Analytics",
                table: "AnalyticsEvents",
                column: "EventCategory");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType",
                schema: "Analytics",
                table: "AnalyticsEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType_CreatedDate",
                schema: "Analytics",
                table: "AnalyticsEvents",
                columns: new[] { "EventType", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId",
                schema: "Analytics",
                table: "AnalyticsEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsEvents",
                schema: "Analytics");
        }
    }
}
