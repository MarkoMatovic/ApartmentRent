using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Applications
{
    /// <inheritdoc />
    public partial class AddPriorityApplicationToApartmentApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "IsPriority",
                schema: "Applications",
                table: "ApartmentApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPriority",
                schema: "Applications",
                table: "ApartmentApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Role_UserRoleId",
                schema: "Applications",
                table: "User",
                column: "UserRoleId",
                principalSchema: "Applications",
                principalTable: "Role",
                principalColumn: "RoleId");
        }
    }
}
