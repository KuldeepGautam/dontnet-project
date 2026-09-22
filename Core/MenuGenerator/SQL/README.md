# MenuGenerator Database Setup — DB-first (2026-07-10, dbo move 2026-07-13)

EF Core migrations were removed from this project on 2026-07-10 in favor of a DB-first workflow:
schema is owned by hand-written/DBA-approved SQL, not `dotnet ef database update`.

**Correction (2026-07-16)**: `01_EfSchema_2026-07-10.sql` used to create a fictional `dbo.M_App`
table — invented in error, never a real table. Checked against the DBA's `UBIS_RBAC.xlsx`: the
real table is the pre-existing legacy **`App_Name`**, sitting alongside `M_Role`/`M_Module`/
`M_Function`/`M_RoleModuleMapping`/`M_RoleFunctionMapping` as a sixth pre-existing legacy `dbo`
table this app maps onto read-only (`ExcludeFromMigrations()` in
`Infrastructure/Persistence/Configurations/AppConfiguration.cs`). The script now creates nothing —
kept only as a changelog pointer. There's no `__EFMigrationsHistory` table: no EF migrations exist
anywhere in this solution as of 2026-07-10, so there's nothing to track.

**This script creates no tables at all now.** `M_Role`/`M_Module`/`M_Function`/
`M_RoleModuleMapping`/`M_RoleFunctionMapping`/`App_Name` are all pre-existing legacy `dbo` tables
this app maps onto read-only (`ExcludeFromMigrations()` in
`Infrastructure/Persistence/Configurations/`) — see `db-scripts/dbo.M_Role.Table.sql` etc. for
those; nothing in this project creates or owns a table.

Going forward: update the entity + Fluent config in code to match the DBA's actual schema (see
`UBIS_RBAC.xlsx`, one sheet per real table) — never invent a new table.

**`02_DevSeedSchemaAndData_2026-07-10.sql`** is a different, unrelated thing: a full schema + seed
data dump for the standalone `MenuDevSeedDbContext` dev/test tool (throwaway local database, kept
separate from and not affecting the real `BIMS2` database). See
`Infrastructure/Persistence/DevSeed/README.md` for details — it doesn't need DBA sign-off.
