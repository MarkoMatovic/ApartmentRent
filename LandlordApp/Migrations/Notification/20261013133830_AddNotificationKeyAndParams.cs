using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Notification
{
    /// <inheritdoc />
    public partial class AddNotificationKeyAndParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                schema: "Notification",
                table: "ReadNotifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParamsJson",
                schema: "Notification",
                table: "ReadNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                schema: "Notification",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParamsJson",
                schema: "Notification",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Key",
                schema: "Notification",
                table: "ReadNotifications");

            migrationBuilder.DropColumn(
                name: "ParamsJson",
                schema: "Notification",
                table: "ReadNotifications");

            migrationBuilder.DropColumn(
                name: "Key",
                schema: "Notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ParamsJson",
                schema: "Notification",
                table: "Notifications");
        }
    }
}
