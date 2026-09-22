# UBIS2 — Union Budget Information System

.NET 10, DB-first EF Core (no migrations — hand-authored idempotent SQL under
`Others/publish-staging/`, gitignored), Clean Architecture, India's Dept. of Economic Affairs.

**Read the area-specific `CLAUDE.md` too** — currently `UBIS_Web/CLAUDE.md` (routing, auth/session,
compliance interceptors, caching). Add one per module as they mature.

## Solution layout

- `Core/` — platform microservices (AIM=auth/users, MenuGenerator, LogWriter, Email, Reporting).
- `Domain/` — business-domain microservices, each its own bounded context: `ECL`, `PreBudget`,
  `SBE` (foundation scaffolded, unstarted beyond that — see `ubis2_sbe_module_plan` memory / the
  saved plan file if picking this up).
- `Shared/` — cross-cutting reference data (`ReferenceData` service: MajorHead/ObjectHead/Scheme/
  SubScheme).
- `UBIS_Web` — MVC front-end, **no business logic, no DB of its own** — everything via typed
  `HttpClient`s to the microservices.
- `Gateway/UBIS.ApiGateway` — Ocelot gateway in front of the microservices.
- `Others/` — gitignored: staging SQL scripts, one-off verification console projects, the
  Playwright `browser-tests` tool (live smoke-testing UBIS_Web end-to-end — see below).

## Repo-wide conventions

- **Per-microservice duplication, not shared packages**: `JwtOptions`, `JwtValidationFactory`,
  `AppSettingsReader`, `CallerRestrictionMiddleware`, `ICacheService`/`CacheService`, `Result<T>`/
  `Error`, and every DTO are each copied independently into every service that needs them. Don't
  extract a shared library — this is deliberate, not an oversight.
- **DB-first, additive-only**: never alter/drop a live column. New tables/columns go in via a
  numbered idempotent script (`Others/publish-staging/<workstream>-N-*.sql`), run against local
  SQL Express first, verified, then the remote Dev Server — same two-step for every change.
- **"Code - Name" display convention**: Scheme/SubScheme/MajorHead/ObjectHead render as
  `"{Code} - {Name}"} in every dropdown, via a static one-method `Infrastructure/Services/
  *DisplayFormatter.cs` per microservice (e.g. PreBudget's `SchemeDisplayFormatter`,
  `MajorHeadDisplayFormatter.NumericSortKey` for numeric-code sort) — reuse the existing formatter
  rather than re-deriving the format inline in a controller.
- **Server-computed totals belong in the entity/EF layer, not client JS**: when a screen shows a
  derived total (sum of BE/RE columns, etc.), add a computed property on the Domain entity and
  thread it through the DTO — don't rely solely on a client-side recalc listener, which silently
  breaks whenever a field is populated programmatically (AJAX prefill, Edit-populate) instead of
  by direct user keystrokes (no `input` event fires). See `AppendixProjectedDemand` (Appendix I-A)
  for the pattern: `PrevYrTotalBE`/`PrevYrTotalRE`/`TotalBE` computed properties, exposed via DTO,
  with the JS's job reduced to just displaying the server-supplied value.
- **Single shared dialog control**: `UBIS_Web/wwwroot/js/dialogs.js` — `window.openInfoDialog(title,
  message)` / `window.openConfirmDialog(title, message, confirmLabel, onConfirm, danger)`, loaded
  globally via `_Layout.cshtml`. Use these for every save/validation/confirm dialog across every
  module (PreBudget, ECL, SBE) instead of ad hoc alerts or per-page markup — this replaced an
  earlier broken reference to a same-named function in `app.js` that was never actually
  `<script>`-included by the real app.
- **Server-side enforcement always, never UI-hiding alone** — role/permission checks, ceiling
  caps, edit-lock flags, etc. must be enforced in the controller/entity, with the UI disabling
  being a convenience on top, not the actual guard.
- **Never let a redirect reach an AJAX fetch() call unguarded.** Root cause of the 2026-08-25
  "page refreshes and loses my Demand/Appendix selection" bug (`ExpiredSessionRedirectAsync`),
  the 2026-08-06 `FreezeAppendixTemplate` bug, and the 2026-08-27 `ComplianceInterceptorFilter`
  recurrence of the same class: `fetch()` follows a 3xx redirect silently and hands the caller
  whatever full HTML page it lands on (Login, ForcePasswordReset, etc.) as if it were the expected
  JSON/fragment/blob — which then either gets rendered as data (a small AJAX container swallowing
  an entire page, looking like an inexplicable "refresh") or silently mis-parsed. This applies to
  **every** code path an AJAX caller can reach, not just the action being written — including
  global filters (`ComplianceInterceptorFilter`, any future cross-cutting filter registered in
  `Program.cs`'s `options.Filters.Add<...>()`) and session/auth guards, since those run before the
  action body and can redirect regardless of what the action itself does. When adding **any** new
  page, controller action, or filter that can be called both by a normal browser navigation and by
  an AJAX `fetch()` (check for `X-Requested-With: XMLHttpRequest`, matching the `IsAjaxRequest()`
  helper already duplicated per-controller — `PreBudgetMeetingController.cs`, all 4 ECL
  controllers):
  - Never `RedirectToAction`/`Redirect(...)` unconditionally on a path an AJAX caller can hit.
    Branch on `IsAjaxRequest()`: AJAX gets a **401** (optionally with a JSON body carrying
    `{ redirectUrl }` when the destination isn't the default `/User/Login` — see
    `ComplianceInterceptorFilter`/`redirectToLoginIfSessionExpired` for the pattern), a normal
    request gets the real `RedirectToActionResult`.
  - On the client, never blindly do `container.innerHTML = <fetched text>` (or `res.json()`
    without checking `res.ok` first) — always check the response status/content-type before
    treating the body as trusted data, the way `ecl.js`'s `downloadExport`/dropdown-cascade fetches
    already do.
  - If a new module introduces its own AJAX-fragment-container page (PreBudget's `#partialViewContainer`
    pattern) or its own `ExpiredSessionRedirectAsync`-style helper, this AJAX-branching has to be
    built in from day one — don't wait for a client bug report to discover it's missing.
- **No test framework in this repo.** Verification is: `dotnet build` clean, then a live check —
  either a scratch EF console project under the Temp scratchpad (querying the real local DB
  directly to prove a query/fix is correct) or the Playwright `Others/browser-tests` tool (logs
  in as a real user, drives the actual UBIS_Web pages, screenshots + grid-row-count comparisons).
  Prefer proving a fix live over stopping at "the code looks right."

## Environments

- **Local SQL**: `DESKTOP-5D5JITM\SQLEXPRESS`, DB `UBIS-Dev`, Windows auth.
- **Remote Dev SQL**: `10.19.116.66,1433`, DB `UBIS-DEV`, SQL auth (see team credentials store —
  not written here).
- **Git remote**: Gitea at `http://10.19.116.66:7070/UBIS/UBIS_DEV` (port 7070 — an older
  `/gitea/UBIS/UBIS_DEV` path on the same host is stale/wrong, do not use it). **This Gitea
  instance is temporary** — the client plans to migrate to an on-premises Azure DevOps at some
  point; don't assume Gitea-specific URLs/tooling are permanent.
- Branch workflow: `gauravmahajan-dev` → `development` → `master`, always merged forward in that
  order after each commit, then all three pushed together. Many other developers' branches exist
  on the same remote (`kishan-dev`, `kushagra-dev`, `muskaan-dev`, `anand_d`, `gulab_d`,
  `mukesh_d`, and more) — treat those as other people's work, don't rewrite them without explicit
  authorization.

## Standing rules

- **Never add a `Co-Authored-By: Claude ...` trailer to any commit message in this repo** (client
  requirement). Existing history on `gauravmahajan-dev`/`development`/`master` was already
  scrubbed of it via a `git filter-branch --msg-filter` pass — don't reintroduce it. See
  `ubis2_no_coauthor_trailer` memory for the full history.
- **PowerShell `$` in `sqlcmd -Q`** silently mangles bcrypt-hash-shaped strings — write SQL to a
  `.sql` file and run `sqlcmd -i` instead of `-Q` for anything with `$`/special characters.
- Check `appsettings.json` for connection-string drift before every commit — a local `dotnet
  publish` overwrites the tracked `DefaultConnection`; run `Restore-ServerAppSettings.ps1` first
  if unsure.
- Redis is wired per-service, not shared: AIM/MenuGenerator/UBIS_Web each have their own
  cache-service instance; PreBudget currently has none.

## Bug-fix verification protocol (mandatory, every fix)

**A bug is NOT considered resolved just because the original visible symptom disappears.** For
every fix in this repo:

1. Reproduce the original bug first — see it fail before touching any code.
2. Identify the root cause — don't patch the symptom without knowing why it happened.
3. Make the smallest possible change.
4. Use a real browser to test the exact failing scenario — the Playwright `Others/browser-tests`
   tool (logs in as a real user, drives the actual UBIS_Web pages) or Chrome DevTools/MCP if
   available; a `dotnet build` passing is not equivalent to this step.
5. Test nearby/related scenarios that use the changed code (other appendices sharing the same
   formatter/validator/JS dispatcher branch, etc.).
6. Refresh the page and repeat the workflow from a clean state (not just the in-memory state left
   over from step 4).
7. Test with at least two different parameter/data combinations (different Demand, different
   Financial Year, different Scheme/record shape, etc.).
8. Check browser console errors and failed network requests, not just the visible UI outcome.
9. If the change affects reporting, separately verify all three: the on-screen report, the PDF
   export, and the XLSX export (`Core/Reporting`).
10. If shared code was modified (a per-microservice-duplicated formatter/validator, the shared
    `pre-budget-meeting.js` dispatcher, `dialogs.js`, etc.), test at least one other feature/report
    that uses it, not just the one that was reported broken.
11. Do not say "resolved" unless all applicable verification steps above actually passed.
12. If anything is unverified, say so explicitly: **"Fix implemented, but full regression
    verification is incomplete."** — never imply full verification happened when it didn't.

## Where to look for more

- `C:\Users\hp\.claude\projects\c--UBIS2\memory\MEMORY.md` — durable cross-session facts/decisions
  (this file's own content is meant to graduate here or vice versa as things stabilize).
- Saved plan file for the SBE module (31-FR breakdown, schema decisions, phasing) — ask for it by
  name if picking up SBE work; nothing beyond the microservice foundation scaffold is built yet.
