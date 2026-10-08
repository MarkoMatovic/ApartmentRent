using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class InitialAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The appointments schema used to be created by hand from Migrations/Appointments/
            // 001_CreateAppointmentsTables.sql, which no code ever ran: a brand-new database ended up
            // with NO appointments tables (DatabaseMigrationService found no migrations to apply),
            // while existing databases already have them. Every statement below is therefore guarded so
            // the migration creates what is missing on a fresh database and is a no-op where the
            // script has already been applied. Definitions mirror what EF scaffolds for the model.
            migrationBuilder.Sql(@"IF SCHEMA_ID(N'appointments') IS NULL EXEC(N'CREATE SCHEMA [appointments];');");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[appointments].[Appointments]', N'U') IS NULL
CREATE TABLE [appointments].[Appointments] (
    [AppointmentId] int NOT NULL IDENTITY,
    [AppointmentGuid] uniqueidentifier NOT NULL DEFAULT (NEWID()),
    [ApartmentId] int NOT NULL,
    [TenantId] int NOT NULL,
    [LandlordId] int NOT NULL,
    [AppointmentDate] datetime2 NOT NULL,
    [Duration] time NOT NULL,
    [Status] int NOT NULL,
    [TenantNotes] nvarchar(500) NULL,
    [LandlordNotes] nvarchar(500) NULL,
    [CreatedByGuid] uniqueidentifier NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedByGuid] uniqueidentifier NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([AppointmentId])
);");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[appointments].[LandlordAvailabilities]', N'U') IS NULL
CREATE TABLE [appointments].[LandlordAvailabilities] (
    [AvailabilityId] int NOT NULL IDENTITY,
    [LandlordId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_LandlordAvailabilities] PRIMARY KEY ([AvailabilityId])
);");

            // Indexes: created only if no index with that name exists yet on the table.
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_ApartmentId", "Appointments", "[ApartmentId]");
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_AppointmentDate", "Appointments", "[AppointmentDate]");
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_AppointmentGuid", "Appointments", "[AppointmentGuid]", unique: true);
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_LandlordId", "Appointments", "[LandlordId]");
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_Status", "Appointments", "[Status]");
            CreateIndexIfMissing(migrationBuilder, "IX_Appointments_TenantId", "Appointments", "[TenantId]");
            CreateIndexIfMissing(migrationBuilder, "IX_LandlordAvailabilities_LandlordId", "LandlordAvailabilities", "[LandlordId]");
            CreateIndexIfMissing(migrationBuilder, "IX_LandlordAvailabilities_LandlordId_DayOfWeek", "LandlordAvailabilities", "[LandlordId], [DayOfWeek]");
        }

        private static void CreateIndexIfMissing(
            MigrationBuilder migrationBuilder, string name, string table, string columns, bool unique = false)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'[appointments].[{table}]'))
CREATE {(unique ? "UNIQUE " : "")}INDEX [{name}] ON [appointments].[{table}] ({columns});");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "LandlordAvailabilities",
                schema: "appointments");
        }
    }
}
