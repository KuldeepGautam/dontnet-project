# UBIS_Web

ASP.NET Core MVC (.NET 10) front-end for the Union Budget Information System. **No business logic
and no database of its own** — every fact comes from a `Core/`/`Shared/`/`Domain/` microservice
over HTTP.

**See also** (not auto-loaded — read only when the task needs them): `../Others/Documentation/NFR_ARCHITECTURE.md`
(solution-wide caching/resilience/RabbitMQ/HTTP-HTTPS/capacity/hosting), `../Others/Documentation/COMPLIANCE_NOTES.md`
(full history behind the compliance interceptors below).

**Identity model (current, 2026-07-13+):** `UserId`/`RoleId` are `int`. Permission claims are
existence-based (`AIM:Function:{FunctionId}`, one per accessible function) via
`SessionPermissionExtensions.HasPermission(int functionId)`; `IsAdmin(adminRoleIds)` gates on
`RoleId`. (An older CRUD-matrix claim shape and a `Guid` identity model are historical — ignore any
code/docs elsewhere still describing those.)

## Architecture

- `Models/` = view models only (what a Razor view needs). No Domain/Infrastructure layers here —
  deliberate, per brief.
- All data access via typed `HttpClient`s in `Services/Clients/` (`IAimClient`, `IMenuClient`, …).
  DTOs are local copies of each service's response shape — no shared contracts project anywhere in
  this solution; every service duplicates its own DTOs, UBIS_Web follows the same convention.
- **Routing = Area/Controller/Action = App/Module/Function**, zero translation layer:
  `AreaSlug` → MVC Area (`Areas/{AreaSlug}/`), `ControllerSlug` → `{ControllerSlug}Controller.cs`,
  `ActionSlug` → action + view at `Views/{ControllerSlug}/{ActionSlug}.cshtml`. MenuGenerator's
  `MenuService.BuildTree` computes these same slugs server-side; the sidebar renders links via
  `asp-area/asp-controller/asp-action`, never a raw `href`.
- **`Tools/MenuScaffolder`** (own `.csproj`, excluded from `UBIS_Web.csproj`'s compile glob):
  idempotent scaffolder that unions every role's (Area,Controller,Action) tuples from AIM+MenuGenerator
  and writes missing controllers/starter views. Existing views are never overwritten; a controller
  only gets new actions appended if it still has the `// SCAFFOLD:APPEND-ACTIONS-ABOVE-THIS-LINE`
  marker. Run after DB Apps/Modules/Functions change:
  `dotnet run -- --aim-url <url> --menu-url <url> --internal-key <key> --output ../../`

## Auth & session

- **Cookie** (`AuthCookie`, default `.UBIS.Auth`): HttpOnly, Secure follows `Security:UseHttps`,
  SameSite=Strict — opaque identity only (`Name`/`FullName`/`RoleName`). **The AIM JWT never
  reaches the browser.**
- **Session** (`SessionOptions`, default `.UBIS.Session`, Redis-backed): real JWT, refresh token,
  permissions, cached menu tree (`UbisSessionData` via `Services/Session/SessionExtensions.cs`).
- **Silent refresh**: `TokenRefreshFilter` (global filter) calls AIM's `/api/authentication/refresh`
  when the token has <~2 min left.
- Login (`UserController.Login`): AIM login → AIM my-role → menu tree (`IAppCacheService`,
  MenuGenerator on cache miss, 5-min cache) → Session → cookie sign-in → Dashboard.
- Both cookies host-only (`Cookie.Domain = null`).

## Caching, resilience, autosave

- `Services/Caching/AppCacheService.cs` — two-tier `IMemoryCache` + Redis (`IDistributedCache`).
- `AimClient`/`MenuClient` resilience pipeline (5 retries, backoff, circuit breaker) — failures are
  caught internally and returned as `ApiCallResult.Failure(503, "SERVICE_UNAVAILABLE")`; callers
  never need a try/catch.
- Offline-safe autosave: add `data-autosave-key="..."` to any `<form>` →
  `wwwroot/js/autosave.js` (localStorage) + `Controllers/Api/DraftController.cs` (Redis). Nothing
  else required.
- `GET /healthz` (anonymous): liveness + `Capacity:TargetConcurrentUsers` + current `Security:UseHttps`.

## Compliance interceptors (full history: `COMPLIANCE_NOTES.md` Pass 2/4)

- **`ComplianceInterceptorFilter`** (global, every authenticated request): forced password reset
  redirect; hardware IP re-check every request via `AppSettingsCache` (`EnableIPLogging`, DB-backed
  — see `Others/publish-staging/create-appsettings-table.sql`), reading the same `dbo.AppSettings`
  row AIM's own login-time check uses; account freeze blocks non-GET while frozen, reads still work.
- `UbisSessionData` carries the compliance snapshot (frozen/allowed-IPs/password-changed-at/etc.),
  populated once at login from AIM `GET /api/users/compliance-status`.
- `AdminController` (+ `Views/Admin/*`) — approve/reject pending change requests, live-editable
  session countdown (`SessionSettings`, written to `IAppCacheService`, read by
  `SessionCountdownViewComponent`), gated on `session.IsAdmin(...)`.
- `session-monitor.js`: (1) idle-triggered logout countdown — starts only after 2 min of no
  activity, any activity cancels it; (2) 5s poll of change-request status → warning banner → forced
  logout on admin approval.
- CAPTCHA (`Services/Captcha/CaptchaService.cs`): server-rendered inline-SVG, answer lives only in
  Redis session, single-use.
- MFA step (`VerifyOtp.cshtml`) exists but is normally dead code — AIM's `Mfa:Enabled` defaults off.

## Config (`appsettings.json`) — DB-backed flags noted where migrated

| Key | Purpose |
|---|---|
| `MicroserviceUrls` | Base URL per service, default `http://localhost:{port}` |
| `InternalCaller:HeaderName`/`SharedKey` | Sent on every AIM/MenuGenerator call; must match each service's `CallerRestriction:SharedClientKey` or every call gets 403'd by `CallerRestrictionMiddleware` |
| `Redis` | Backs distributed Session + `IAppCacheService` L2 |
| `RabbitMq:LogExchange`/`LogRoutingKeyPrefix` | Publishes `log.{level}` for LogWriter |
| `Resilience:MaxRetryAttempts` | Default 5 |
| `Capacity:TargetConcurrentUsers` | Documented target (1000), surfaced on `/healthz` — not enforced |
| `Draft:TtlHours` | Autosave draft TTL (default 48h) |
| `SessionOptions`/`AuthCookie` | Cookie names/timeouts; `CountdownMinutes` default 15 |
| `DefaultFinancialYear` | Login form default |
| ~~`Security:UseHttps`~~ | Moved to `dbo.AppSettings.EnableHttps` (2026-08-07), read via AIM's `api/app-settings` (`AppSettingsCache`) since UBIS_Web has no direct DB access |
| ~~`Security:EnableUserIPSettings`~~ | Moved to `dbo.AppSettings.EnableIPLogging`, same mechanism |
| ~~`Mfa:PhoneOtpEnabled`~~ | Moved to `dbo.AppSettings.EnableSms` |

## Security posture

HSTS/HTTPS-redirect when `EnableHttps` on; CSP + `X-Content-Type-Options` + `X-Frame-Options: DENY`
+ `Referrer-Policy` + `Permissions-Policy` always. `Cache-Control: no-store` once authenticated.
Antiforgery on every POST + the autosave AJAX header token. Fixed-window login rate limit per IP
(defense-in-depth on top of AIM's own lockout). Data Protection keys in `App_Data/DataProtection-Keys`
(single-server default; point at shared storage/Redis for a real farm).

## Known gaps / out of scope

- AIM's admin endpoints beyond `GetMyRole` still lack `[Authorize]` (Phase-0 scope was `GetMyRole` only).
- Ports pinned per `launchSettings.json`: AIM 5000, MenuGenerator 5001, LogWriter 5002, PreBudget
  5010, Gateway 5150, Email 5004, UserProfile 5104, UBIS_Web 5100. AIM/MenuGenerator live on IIS at
  `172.18.160.1`; everything else routes through the Gateway or runs locally — see
  `Gateway/UBIS.ApiGateway/Configure-Gateway.ps1`.
