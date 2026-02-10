using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddTenantLandlordHybridRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TenantLandlord hybrid role
            var newRole = @"
                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'TenantLandlord')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('TenantLandlord', 'User who is both a tenant and landlord', GETDATE());
                END
            ";
            
            migrationBuilder.Sql(newRole);

            // Add permissions for TenantLandlord (combination of Tenant + Landlord)
            var rolePermissions = @"
                DECLARE @TenantLandlordRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'TenantLandlord');
                
                -- Combine all permissions from Tenant and Landlord
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @TenantLandlordRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    -- Apartment permissions (Landlord)
                    'apartments.create', 'apartments.edit.own', 'apartments.delete.own', 'apartments.publish',
                    -- User permissions
                    'users.edit.own',
                    -- Application permissions (both)
                    'applications.create', 'applications.view.own', 'applications.view.received', 'applications.manage',
                    -- Communication permissions
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    -- Review permissions
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    -- Roommate permissions (Tenant)
                    'roommates.create', 'roommates.edit.own', 'roommates.search', 'roommates.match',
                    -- Appointment permissions (both)
                    'appointments.create', 'appointments.view.own', 'appointments.view.received', 'appointments.manage',
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
