# Email Microservice Database Setup — DB-first (2026-07-10, dbo move 2026-07-13)

EF Core migrations were removed from this project on 2026-07-10 in favor of a DB-first workflow:
schema is owned by hand-written/DBA-approved SQL, not `dotnet ef database update`.

`01_EfSchema_2026-07-10.sql` creates `dbo.M_EmailDispatchLog` (moved from a Guid-keyed
`emailservice.EmailDispatchLog` table on 2026-07-13 — int identity, single shared `BIMS2` database
per the identity-model rewrite; see `COMPLIANCE_NOTES.md`). `MessageId` stays a `uniqueidentifier`
— it's the RabbitMQ message correlation id, not a row identity. User-confirmed 2026-07-13: this
table deliberately carries none of the standard 7-column audit template other new tables get — a
pure email-send log, not a user-facing/audited record. It's a plain idempotent script
(`IF OBJECT_ID(...) IS NULL` guard, safe to re-run) — **get DBA sign-off, then run it once.**
There's no `__EFMigrationsHistory` table anymore: no EF migrations exist anywhere in this solution
as of 2026-07-10, so there's nothing to track.

Going forward: update the entity + `EmailDbContext` config in code, then hand-write (and get DBA
approval for) the matching `ALTER`/`CREATE` SQL. No `dotnet ef migrations add` anymore.
