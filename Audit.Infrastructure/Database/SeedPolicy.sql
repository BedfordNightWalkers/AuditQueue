USE AuditQueue;
GO

DECLARE @PolicyId UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Policies
(
    Id,
    ClientId,
    PackageId,
    QuoteRef,
    PolicyNumber,
    Status,
    UpdatedAtUtc
)
VALUES
(
    @PolicyId,
    'underwriter-001',
    'motor-package-001',
    'ABC123',
    'POL-100001',
    'Quoted',
    SYSUTCDATETIME()
);

SELECT
    Id,
    ClientId,
    PackageId,
    QuoteRef,
    PolicyNumber,
    Status,
    UpdatedAtUtc
FROM dbo.Policies
WHERE Id = @PolicyId;