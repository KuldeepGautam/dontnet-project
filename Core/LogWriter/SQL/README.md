# LogWriter Database Setup — DB-first (2026-07-10, dbo move 2026-07-13)

EF Core migrations were removed from this project on 2026-07-10 in favor of a DB-first workflow:
schema is owned by hand-written/DBA-approved SQL, not `dotnet ef database update`.

`01_EfSchema_2026-07-10.sql` creates `dbo.M_LogEntry` (moved from a Guid-keyed `logwriter.Logs`
table on 2026-07-13 — int identity, standard 7-column audit template added, single shared `BIMS2`
database per the identity-model rewrite; see `COMPLIANCE_NOTES.md`). It's a plain idempotent
script (`IF OBJECT_ID(...) IS NULL` guard, safe to re-run) — **get DBA sign-off, then run it
once.** There's no `__EFMigrationsHistory` table anymore: no EF migrations exist anywhere in this
solution as of 2026-07-10, so there's nothing to track.

Going forward: update the entity + `LogDbContext` config in code, then hand-write (and get DBA
approval for) the matching `ALTER`/`CREATE` SQL. No `dotnet ef migrations add` anymore.
