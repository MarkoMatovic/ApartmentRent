using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddRoommateRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Roommate role
            var newRole = @"
                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Roommate')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Roommate', 'User looking for a roommate to share an apartment', GETDATE());
                END
            ";
            
            migrationBuilder.Sql(newRole);

            // Add permissions for Roommate role
            var rolePermissions = @"
                DECLARE @RoommateRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Roommate');
                
                -- Roommate role has similar permissions to Tenant, focused on finding roommates
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @RoommateRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    -- User permissions
                    'users.edit.own',
                    -- Application permissions (can apply to apartments)
                    'applications.create', 'applications.view.own',
                    -- Communication permissions
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    -- Review permissions
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    -- Roommate permissions (primary focus)
                    'roommates.create', 'roommates.edit.own', 'roommates.search', 'roommates.match',
                    -- Appointment permissions
                    'appointments.create', 'appointments.view.own',
                    -- Favorites
                    'favorites.add', 'favorites.remove', 'favorites.view'
                );
            ";
            
            migrationBuilder.Sql(rolePermissions);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
