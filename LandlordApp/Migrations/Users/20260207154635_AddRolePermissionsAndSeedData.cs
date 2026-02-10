using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddRolePermissionsAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "UsersRoles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedByGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RolePerm__RolePermission", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK__RolePerm__PermId",
                        column: x => x.PermissionId,
                        principalSchema: "UsersRoles",
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RolePerm__RoleId",
                        column: x => x.RoleId,
                        principalSchema: "UsersRoles",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "UsersRoles",
                table: "RolePermissions",
                column: "PermissionId");

            // =====================================================
            // SEED DATA: PERMISSIONS
            // =====================================================
            
            var permissions = @"
                -- Apartment Management Permissions
                INSERT INTO [UsersRoles].[Permissions] ([PermissionName], [Description], [CreatedDate]) VALUES 
                ('apartments.create', 'Create new apartment listings', GETDATE()),
                ('apartments.edit.own', 'Edit own apartment listings', GETDATE()),
                ('apartments.edit.any', 'Edit any apartment listing', GETDATE()),
                ('apartments.delete.own', 'Delete own apartment listings', GETDATE()),
                ('apartments.delete.any', 'Delete any apartment listing', GETDATE()),
                ('apartments.view.all', 'View all apartments including inactive', GETDATE()),
                ('apartments.publish', 'Publish apartment listings', GETDATE()),

                -- User Management Permissions
                ('users.view.all', 'View all users in the system', GETDATE()),
                ('users.edit.own', 'Edit own user profile', GETDATE()),
                ('users.edit.any', 'Edit any user profile', GETDATE()),
                ('users.delete', 'Delete user accounts', GETDATE()),
                ('users.ban', 'Ban/unban users', GETDATE()),
                ('users.roles.manage', 'Manage user roles', GETDATE()),

                -- Application Permissions
                ('applications.create', 'Submit apartment applications', GETDATE()),
                ('applications.view.own', 'View own applications', GETDATE()),
                ('applications.view.received', 'View received applications (landlord)', GETDATE()),
                ('applications.manage', 'Accept/reject applications', GETDATE()),

                -- Communication Permissions
                ('messages.send', 'Send messages to other users', GETDATE()),
                ('messages.view.own', 'View own messages', GETDATE()),
                ('messages.view.all', 'View all messages (moderation)', GETDATE()),
                ('messages.delete.own', 'Delete own messages', GETDATE()),
                ('messages.delete.any', 'Delete any message', GETDATE()),

                -- Review Permissions
                ('reviews.create', 'Create reviews', GETDATE()),
                ('reviews.edit.own', 'Edit own reviews', GETDATE()),
                ('reviews.delete.own', 'Delete own reviews', GETDATE()),
                ('reviews.delete.any', 'Delete any review', GETDATE()),
                ('reviews.moderate', 'Moderate reviews', GETDATE()),

                -- Analytics Permissions
                ('analytics.view.personal', 'View personal analytics', GETDATE()),
                ('analytics.view.landlord', 'View landlord analytics', GETDATE()),
                ('analytics.view.system', 'View system-wide analytics', GETDATE()),
                ('analytics.ml.manage', 'Manage ML models', GETDATE()),

                -- Roommate Permissions
                ('roommates.create', 'Create roommate profile', GETDATE()),
                ('roommates.edit.own', 'Edit own roommate profile', GETDATE()),
                ('roommates.search', 'Search for roommates', GETDATE()),
                ('roommates.match', 'Access roommate matching', GETDATE()),

                -- Appointment Permissions
                ('appointments.create', 'Schedule apartment viewings', GETDATE()),
                ('appointments.view.own', 'View own appointments', GETDATE()),
                ('appointments.view.received', 'View received appointment requests', GETDATE()),
                ('appointments.manage', 'Accept/reject appointments', GETDATE()),

                -- Favorites Permissions
                ('favorites.add', 'Add apartments to favorites', GETDATE()),
                ('favorites.remove', 'Remove apartments from favorites', GETDATE()),
                ('favorites.view', 'View favorite apartments', GETDATE());
            ";

            // Execute permission inserts
            migrationBuilder.Sql(permissions);

            // =====================================================
            // SEED DATA: NEW ROLES (Landlord, Premium Tenant, Premium Landlord, Moderator)
            // Admin, Tenant, and Guest already exist
            // =====================================================
            
            var newRoles = @"
                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Landlord')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Landlord', 'Property owner who can list and manage apartments', GETDATE());
                END

                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Premium Tenant')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Premium Tenant', 'Premium tenant with additional features', GETDATE());
                END

                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Premium Landlord')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Premium Landlord', 'Premium landlord with analytics and ML features', GETDATE());
                END

                IF NOT EXISTS (SELECT 1 FROM [UsersRoles].[Roles] WHERE [RoleName] = 'Moderator')
                BEGIN
                    INSERT INTO [UsersRoles].[Roles] ([RoleName], [Description], [CreatedDate])
                    VALUES ('Moderator', 'Content moderator with limited admin rights', GETDATE());
                END
            ";

            migrationBuilder.Sql(newRoles);

            // =====================================================
            // SEED DATA: ROLE-PERMISSION MAPPINGS
            // =====================================================
            
            var rolePermissions = @"
                -- ========================
                -- ADMIN ROLE (All permissions)
                -- ========================
                DECLARE @AdminRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Admin');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @AdminRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions];

                -- ========================
                -- LANDLORD ROLE
                -- ========================
                DECLARE @LandlordRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Landlord');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @LandlordRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'apartments.create', 'apartments.edit.own', 'apartments.delete.own', 'apartments.publish',
                    'users.edit.own',
                    'applications.view.received', 'applications.manage',
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    'appointments.view.received', 'appointments.manage',
                    'favorites.add', 'favorites.remove', 'favorites.view'
                );

                -- ========================
                -- TENANT ROLE
                -- ========================
                DECLARE @TenantRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Tenant');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @TenantRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'users.edit.own',
                    'applications.create', 'applications.view.own',
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    'roommates.create', 'roommates.edit.own', 'roommates.search', 'roommates.match',
                    'appointments.create', 'appointments.view.own',
                    'favorites.add', 'favorites.remove', 'favorites.view'
                );

                -- ========================
                -- PREMIUM TENANT ROLE (Tenant + Analytics)
                -- ========================
                DECLARE @PremiumTenantRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Premium Tenant');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @PremiumTenantRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'users.edit.own',
                    'applications.create', 'applications.view.own',
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    'analytics.view.personal',
                    'roommates.create', 'roommates.edit.own', 'roommates.search', 'roommates.match',
                    'appointments.create', 'appointments.view.own',
                    'favorites.add', 'favorites.remove', 'favorites.view'
                );

                -- ========================
                -- PREMIUM LANDLORD ROLE (Landlord + Analytics + ML)
                -- ========================
                DECLARE @PremiumLandlordRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Premium Landlord');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @PremiumLandlordRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'apartments.create', 'apartments.edit.own', 'apartments.delete.own', 'apartments.publish',
                    'users.edit.own',
                    'applications.view.received', 'applications.manage',
                    'messages.send', 'messages.view.own', 'messages.delete.own',
                    'reviews.create', 'reviews.edit.own', 'reviews.delete.own',
                    'analytics.view.landlord', 'analytics.ml.manage',
                    'appointments.view.received', 'appointments.manage',
                    'favorites.add', 'favorites.remove', 'favorites.view'
                );

                -- ========================
                -- MODERATOR ROLE
                -- ========================
                DECLARE @ModeratorRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Moderator');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @ModeratorRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'apartments.view.all', 'apartments.edit.any', 'apartments.delete.any',
                    'users.view.all', 'users.edit.own', 'users.ban',
                    'messages.view.all', 'messages.delete.any',
                    'reviews.moderate', 'reviews.delete.any'
                );

                -- ========================
                -- GUEST ROLE (Read-only)
                -- ========================
                DECLARE @GuestRoleId INT = (SELECT RoleId FROM [UsersRoles].[Roles] WHERE RoleName = 'Guest');
                
                INSERT INTO [UsersRoles].[RolePermissions] (RoleId, PermissionId, CreatedDate)
                SELECT @GuestRoleId, PermissionId, GETDATE()
                FROM [UsersRoles].[Permissions]
                WHERE PermissionName IN (
                    'favorites.view'
                );
            ";

            migrationBuilder.Sql(rolePermissions);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "UsersRoles");
        }
    }
}
