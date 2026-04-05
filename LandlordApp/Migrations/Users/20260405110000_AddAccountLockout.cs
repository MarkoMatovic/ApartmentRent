using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddAccountLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'FailedLoginAttempts'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'LockoutUntil'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [LockoutUntil] datetime2 NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
