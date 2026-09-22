# AIM Microservice Database Setup — DB-first (SQL Server only)

AIM shares one SQL Server database, `BIMS2`, with every other UBIS 2.0 service (no PostgreSQL —
never actually wired into any provider-switch code, just unused config placeholders that have
since been removed). There are no EF Core migrations anywhere in this solution: schema is owned by
hand-written, DBA-reviewed SQL.

**The real schema lives in `db-scripts/` at the repo root, not in this folder.** See
`db-scripts/README.md` for the full apply order:

1. `dbo.M_Role.Table.sql`, `dbo.M_Module.Table.sql`, `dbo.M_Function.Table.sql`,
   `dbo.M_RoleModuleMapping.Table.sql`, `dbo.M_RoleFunctionMapping.Table.sql` — legacy tables
   (DBA exports), int-keyed, no CRUD columns (existence of a Role→Function mapping row = access).
2. `M_UsersNew.sql` — the DBA's real `dbo.M_Users` export (int `IDENTITY`, one row per person per
   financial year).
3. `M_Users_ComplianceColumns.md`, `M_Users_HashDefaultPassword.md` — compliance-brief columns and
   the one-time default-password-to-BCrypt-hash conversion.
4. `M_SecurityEvent.Table.sql`, `M_PasswordHistory.Table.sql` — brand-new tables, no legacy
   equivalent, int-keyed to `M_Users.UserId`, full 7-column audit template.
5. `UserId_StmtId_Mapping.md` — non-owner Statement/"Profile" assignment table.

Every AIM entity config uses `.ToTable(..., "dbo", t => t.ExcludeFromMigrations())` — EF Core will
never generate or apply DDL for any of these. Going forward: update the entity + Fluent config in
code, then hand-write (and get DBA approval for) the matching `ALTER`/`CREATE` SQL under
`db-scripts/`. No `dotnet ef migrations add`, ever.

## Connection string

All 5 services point at the same database via each project's `appsettings.json`
(`ConnectionStrings:SqlServer:DefaultConnection` for AIM):

```
Server=DESKTOP-5D5JITM\SQLEXPRESS;Database=BIMS2;Encrypt=true;TrustServerCertificate=true;Connection Timeout=30;
```

## Legacy pass-throughs (not created by any script here — pre-existing, `dbo` schema)

Under `Domain/Entities/Legacy/` + `Infrastructure/Persistence/Configurations/Legacy/`, all
`ExcludeFromMigrations()` (read/write mappings onto tables the legacy BIMS system already owns,
int-keyed to `M_Users.UserId` directly — no bridge table): `UserDetails` (→ `dbo.UserDetails`,
User↔Demand "Profile Access"), `Section` (→ `dbo.Section`), `StmtControl` (→ `dbo.M_StmtControl`,
Statement ownership), `SBEDemand` (→ `dbo.M_SBEDemand`), `UserIPRequest` (→ `dbo.M_UserIPrequest`,
also written to — the Mobile/IP change-request workflow). Full rationale in `COMPLIANCE_NOTES.md`
at the repo root.
