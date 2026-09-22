# MenuDevSeedDbContext — DB-first (2026-07-10)

This context's EF Core migrations were removed on 2026-07-10, for consistency with the rest of the
solution's DB-first shift — though note this context is different in kind from the other four:
it was never a *production* schema owner, just a standalone tool that builds a **throwaway local
dev/test database** (`MenuGeneratorDevSeed`, a separate connection string from the real `menu`
schema) fully seeded with realistic RBAC data (14 Apps / ~100 Modules / ~600 Functions, from the
original `UBIS_RBAC.xlsx`).

**`Core/MenuGenerator/SQL/02_DevSeedSchemaAndData_2026-07-10.sql`** is the full schema + seed data
this context's model expects, generated once (`dotnet ef migrations script --idempotent`) from the
last EF migration before removal. Run it against your own local `MenuGeneratorDevSeed` database
whenever you want a fresh, fully-seeded copy to develop/test against — this one's a developer
convenience, not a production deployment artifact, so it doesn't need the same DBA sign-off as
`Core/MenuGenerator/SQL/01_EfSchema_2026-07-10.sql` (the real `menu` schema). Use your own judgment
if your team wants DBA review anyway.

Going forward: update the entity/seed configuration in code, then regenerate the script by hand
(there's no `dotnet ef migrations add` anymore) — or just re-run the whole 2,638-line script
against a dropped-and-recreated `MenuGeneratorDevSeed` database, since it's throwaway by design.
