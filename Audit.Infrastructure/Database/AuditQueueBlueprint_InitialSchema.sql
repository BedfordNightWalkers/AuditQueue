/*
    AuditQueue - initial SQL schema
    Database: AuditQueue (SQL Server LocalDB)
    Alternative to EF Core migrations. Choose migrations OR this script as the schema owner;
    do not apply both independently to the same empty database without reconciling migration history.
*/
IF DB_ID(N'AuditQueue') IS NULL
BEGIN
    CREATE DATABASE [AuditQueue];
END;
GO

USE [AuditQueue];
GO

CREATE TABLE dbo.Policies
(
    Id              UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_Policies PRIMARY KEY,
    ClientId        NVARCHAR(100) NOT NULL,
    PackageId       NVARCHAR(100) NOT NULL,
    QuoteRef        NVARCHAR(100) NOT NULL,
    PolicyNumber    NVARCHAR(100) NOT NULL,
    Status          NVARCHAR(30) NOT NULL,
    UpdatedAtUtc    DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_Policies_UpdatedAtUtc DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_Policies_ClientId_PackageId
    ON dbo.Policies (ClientId, PackageId);
GO

CREATE INDEX IX_Policies_PolicyNumber
    ON dbo.Policies (PolicyNumber);
GO

CREATE TABLE dbo.OutboxMessages
(
    Id              UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_OutboxMessages PRIMARY KEY,
    EventId         NVARCHAR(64) NOT NULL,
    EventType       NVARCHAR(200) NOT NULL,
    PayloadJson     NVARCHAR(MAX) NOT NULL,
    CreatedAtUtc    DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_OutboxMessages_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    PublishedAtUtc  DATETIMEOFFSET(7) NULL,
    Attempts        INT NOT NULL
        CONSTRAINT DF_OutboxMessages_Attempts DEFAULT (0),
    LastError       NVARCHAR(2000) NULL
);
GO

CREATE UNIQUE INDEX UX_OutboxMessages_EventId
    ON dbo.OutboxMessages (EventId);
GO

CREATE INDEX IX_OutboxMessages_Pending
    ON dbo.OutboxMessages (CreatedAtUtc)
    WHERE PublishedAtUtc IS NULL;
GO
