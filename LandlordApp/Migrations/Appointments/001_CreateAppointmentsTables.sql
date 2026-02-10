-- Create Appointments Schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'appointments')
BEGIN
    EXEC('CREATE SCHEMA appointments')
END
GO

-- Create Appointments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments' AND schema_id = SCHEMA_ID('appointments'))
BEGIN
    CREATE TABLE appointments.Appointments (
        AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
        AppointmentGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        ApartmentId INT NOT NULL,
        TenantId INT NOT NULL,
        LandlordId INT NOT NULL,
        AppointmentDate DATETIME2 NOT NULL,
        Duration TIME NOT NULL DEFAULT '00:30:00',
        [Status] INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Confirmed, 2=Cancelled, 3=Completed, 4=Rejected
        TenantNotes NVARCHAR(500) NULL,
        LandlordNotes NVARCHAR(500) NULL,
        CreatedByGuid UNIQUEIDENTIFIER NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedByGuid UNIQUEIDENTIFIER NULL,
        ModifiedDate DATETIME2 NULL,
        
        CONSTRAINT UQ_Appointments_Guid UNIQUE (AppointmentGuid)
    );
    
    -- Create Indexes
    CREATE INDEX IX_Appointments_ApartmentId ON appointments.Appointments(ApartmentId);
    CREATE INDEX IX_Appointments_TenantId ON appointments.Appointments(TenantId);
    CREATE INDEX IX_Appointments_LandlordId ON appointments.Appointments(LandlordId);
    CREATE INDEX IX_Appointments_AppointmentDate ON appointments.Appointments(AppointmentDate);
    CREATE INDEX IX_Appointments_Status ON appointments.Appointments([Status]);
END
GO

-- Create LandlordAvailabilities Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandlordAvailabilities' AND schema_id = SCHEMA_ID('appointments'))
BEGIN
    CREATE TABLE appointments.LandlordAvailabilities (
        AvailabilityId INT IDENTITY(1,1) PRIMARY KEY,
        LandlordId INT NOT NULL,
        DayOfWeek INT NOT NULL, -- 0=Sunday, 1=Monday, etc.
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL
    );
    
    -- Create Indexes
    CREATE INDEX IX_LandlordAvailabilities_LandlordId ON appointments.LandlordAvailabilities(LandlordId);
    CREATE INDEX IX_LandlordAvailabilities_LandlordId_DayOfWeek ON appointments.LandlordAvailabilities(LandlordId, DayOfWeek);
END
GO

PRINT 'Appointments module tables created successfully!'
