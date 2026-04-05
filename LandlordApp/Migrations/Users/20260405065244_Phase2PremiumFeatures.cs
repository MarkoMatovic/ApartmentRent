using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class Phase2PremiumFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist from a partial previous run
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'IsIncognito'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [IsIncognito] bit NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[UsersRoles].[Users]')
                    AND name = 'TokenBalance'
                )
                BEGIN
                    ALTER TABLE [UsersRoles].[Users]
                    ADD [TokenBalance] int NOT NULL DEFAULT 3
                END
            ");

            // RefreshTokens table (also idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('UsersRoles') AND name = 'RefreshTokens')
                BEGIN
                    CREATE TABLE [UsersRoles].[RefreshTokens] (
                        [Id]         int          NOT NULL IDENTITY(1,1),
                        [TokenHash]  nvarchar(64) NOT NULL,
                        [UserId]     int          NOT NULL,
                        [ExpiresAt]  datetime2    NOT NULL,
                        [CreatedAt]  datetime2    NOT NULL DEFAULT (getutcdate()),
                        [IsRevoked]  bit          NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_RefreshTokens_Users_UserId]
                            FOREIGN KEY ([UserId]) REFERENCES [UsersRoles].[Users]([UserId])
                            ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [UsersRoles].[RefreshTokens] ([TokenHash]);
                    CREATE INDEX [IX_RefreshTokens_UserId] ON [UsersRoles].[RefreshTokens] ([UserId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "UsersRoles");

            migrationBuilder.DropColumn(
                name: "IsIncognito",
                schema: "UsersRoles",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TokenBalance",
                schema: "UsersRoles",
                table: "Users");
        }
    }
}
