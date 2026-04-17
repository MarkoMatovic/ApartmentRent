using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddRowVersionToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FailedLoginAttempts",
                schema: "UsersRoles",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "UsersRoles",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "FailedLoginAttempts",
                schema: "UsersRoles",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
