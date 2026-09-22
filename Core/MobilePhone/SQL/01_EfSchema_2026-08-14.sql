-- dbo.M_SmsDispatchLog — SMS equivalent of Email's dbo.M_EmailDispatchLog, no legacy equivalent,
-- owned by MobilePhone. Same rationale as M_EmailDispatchLog: a pure SMS-send log, not a
-- user-facing/audited record, so it deliberately carries none of the standard 7-column audit
-- template other new tables get — just an int identity PK. No EF migrations exist anywhere in
-- this solution, so this is a plain idempotent script, not a migrations-history-tracked one.
IF OBJECT_ID(N'[dbo].[M_SmsDispatchLog]') IS NULL
BEGIN
    CREATE TABLE [dbo].[M_SmsDispatchLog] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [ToMobileMasked] nvarchar(20) NOT NULL,
        [Purpose] nvarchar(32) NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [ProviderResponseCode] nvarchar(32) NULL,
        [SentAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_M_SmsDispatchLog] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_M_SmsDispatchLog_MessageId] ON [dbo].[M_SmsDispatchLog] ([MessageId]);
END;
GO
