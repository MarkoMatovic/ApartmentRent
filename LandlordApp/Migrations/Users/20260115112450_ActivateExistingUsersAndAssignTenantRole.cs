using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class ActivateExistingUsersAndAssignTenantRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kreiraj rolu "Tenant" ako ne postoji
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Tenant')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Tenant', 'Tenant role for users looking for apartments', GETDATE());
                END
            ");

            // Aktiviraj sve postojeće korisnike
            migrationBuilder.Sql(@"
                UPDATE [UsersRoles].[Users]
                SET [IsActive] = 1
                WHERE [IsActive] = 0;
            ");

            // Dodeli rolu "Tenant" svim korisnicima koji nemaju rolu
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.[UserRoleId] = r.[RoleId]
                FROM [UsersRoles].[Users] u
                CROSS APPLY (
                    SELECT TOP 1 [RoleId]
                    FROM [UsersRoles].[Roles]
                    WHERE [RoleName] = 'Tenant'
                ) r
                WHERE u.[UserRoleId] IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback nije potreban - ovo je data migration
        }
    }
}
