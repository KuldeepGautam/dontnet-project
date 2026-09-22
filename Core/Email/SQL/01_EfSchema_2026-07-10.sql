-- dbo.M_EmailDispatchLog — no legacy equivalent, owned by Email. Moved from a Guid-keyed
-- `emailservice.EmailDispatchLog` table to `dbo` (int identity) on 2026-07-13, per the
-- identity-model rewrite (single shared `BIMS2` database, no per-service schemas). User-confirmed
-- 2026-07-13: this is a pure email-send log, not a user-facing/audited record, so it deliberately
-- carries none of the standard 7-column audit template other new tables get — just an int
-- identity PK. No EF migrations exist anywhere in this solution (removed 2026-07-10), so this is a
-- plain idempotent script, not a migrations-history-tracked one.
IF OBJECT_ID(N'[dbo].[M_EmailDispatchLog]') IS NULL
BEGIN
    CREATE TABLE [dbo].[M_EmailDispatchLog] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [ToEmailMasked] nvarchar(320) NOT NULL,
        [Purpose] nvarchar(32) NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [ProviderResponseCode] nvarchar(32) NULL,
        [SentAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_M_EmailDispatchLog] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_M_EmailDispatchLog_MessageId] ON [dbo].[M_EmailDispatchLog] ([MessageId]);
END;
GO
