# MobilePhone Microservice Database Setup — DB-first (2026-08-14)

Mirrors Email's DB-first workflow (`Core/Email/SQL/README.md`): schema is owned by
hand-written/DBA-approved SQL, not `dotnet ef database update`. No EF migrations exist anywhere
in this solution.

`01_EfSchema_2026-08-14.sql` creates `dbo.M_SmsDispatchLog` — the SMS equivalent of
`dbo.M_EmailDispatchLog`. `MessageId` is a `uniqueidentifier` — it's the RabbitMQ message
correlation id, not a row identity. Like `M_EmailDispatchLog`, this table deliberately carries
none of the standard 7-column audit template other new tables get — a pure SMS-send log, not a
user-facing/audited record. It's a plain idempotent script (`IF OBJECT_ID(...) IS NULL` guard,
safe to re-run) — **get DBA sign-off, then run it once** against local `UBIS-Dev` first, then the
remote Dev Server.

Going forward: update the entity + `MobilePhoneDbContext` config in code, then hand-write (and
get DBA approval for) the matching `ALTER`/`CREATE` SQL. No `dotnet ef migrations add`.
