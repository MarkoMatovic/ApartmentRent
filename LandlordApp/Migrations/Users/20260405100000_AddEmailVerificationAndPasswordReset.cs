using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist from a partial previous run
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'EmailVerificationToken'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [EmailVerificationToken] nvarchar(200) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'EmailVerifiedAt'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [EmailVerifiedAt] datetime2 NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'PasswordResetToken'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [PasswordResetToken] nvarchar(200) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'PasswordResetTokenExpiry'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [PasswordResetTokenExpiry] datetime2 NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
