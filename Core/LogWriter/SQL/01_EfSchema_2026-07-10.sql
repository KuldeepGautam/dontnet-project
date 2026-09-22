-- dbo.M_LogEntry — no legacy equivalent, owned by LogWriter. Moved from a Guid-keyed
-- `logwriter.Logs` table to `dbo` (int identity + standard audit columns) on 2026-07-13, per the
-- identity-model rewrite (single shared `BIMS2` database, no per-service schemas). No EF
-- migrations exist anywhere in this solution (removed 2026-07-10), so this is a plain idempotent
-- script, not a migrations-history-tracked one.
IF OBJECT_ID(N'[dbo].[M_LogEntry]') IS NULL
BEGIN
    CREATE TABLE [dbo].[M_LogEntry] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Level] nvarchar(50) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Exception] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsActive] bit NULL,
        [UserIdCreatedBy] int NULL,
        [CreatedOnDate] datetime NULL,
        [UserIdModifyBy] int NULL,
        [ModifiedOnDate] datetime NULL,
        [UserIdDeletedBy] int NULL,
        [DeletedOnDate] datetime NULL,
        CONSTRAINT [PK_M_LogEntry] PRIMARY KEY ([Id])
    );
END;
GO
