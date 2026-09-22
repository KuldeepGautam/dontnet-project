# AIM Module — Addendum Spec 02: Menu/RBAC Query APIs, Role/Email Lookup APIs & Service-to-Service Caller Restriction

**Status:** Incremental change request — extends Addendum 01, does **not** modify `.claude` or Addendum 01.
**How to use this file:** Hand to Claude Code as a third prompt in the same repo. Instruct it to add the new controllers/settings below to the existing AIM solution, and to add the caller-restriction middleware to **every** microservice solution (AIM, EmailService, and any others already generated) — not just AIM.
**Grounding:** Field names below (`M_Module.Display_SequenceNo`, `M_RoleModuleMapping.RM_SequenceNo`, `M_RoleFunctionMapping.RF_SequenceNo`, etc.) are taken directly from the validated `UBIS_RBAC.xlsx` / `UserRelatedDataExport.xlsx` extracts, per FR-004.

---

## 1. Full API List (this addendum)

| # | Requested name | Method & Route | Purpose |
|---|---|---|---|
| 1 | Login | `POST /api/auth/login` + `/login/verify-otp` | Already specified in Addendum 01 §3 — no change here. |
| 2 | PasswordReset | `POST /api/auth/password-setup/*` and `POST /api/auth/password-reset/*` | Already specified in Addendum 01 §4–§5. "PasswordReset" as you named it covers **both** the first-time-setup case (no password yet) and the existing-user reset case — Addendum 01 splits these into two endpoint families because the preconditions differ (null hash vs. existing hash); Claude Code should not merge them into one endpoint. |
| 3 | ForgetPassword | `POST /api/auth/password-reset/request` (alias/UI label "Forgot Password") | Same endpoint family as #2, existing-user branch. No new endpoint needed — just the UI-facing name differs from the route name. |
| 4 | ModuleListForUserId | `GET /api/menu/modules` | New — see §2.1 |
| 5 | FunctionNamesForUserIdAndModuleName | `GET /api/menu/functions` | New — see §2.2 |
| 6 | RetrieveMenuForUserId | `GET /api/menu/full` | New — see §2.3 |
| 7 | getRoleOfUser | `GET /api/users/role` | New — see §2.4 |
| 8 | getEmailOfUser | `GET /api/users/email` | New — see §2.5 |

All five new endpoints (#4–#8) live in a new `AIM.WebApi/Controllers/MenuController.cs` (for #4–#6) and `AIM.WebApi/Controllers/UserLookupController.cs` (for #7–#8).

---

## 2. New Endpoint Specifications

### General rules for all five (§2.1–§2.5)
- **Transport:** REST/JSON over HTTPS only (`RequireHttps` in Kestrel/hosting config); no plaintext HTTP even inside the internal network.
- **AuthN:** `[Authorize]` with the existing JWT bearer scheme from `.claude` §3/§6 — every call must present a valid, non-revoked (Redis-checked) access token.
- **AuthZ / IDOR guard:** Do **not** trust a `userId` query/route parameter blindly. Resolve the caller's own `UserId` from the JWT `sub`/`UserId` claim first. Only allow querying a **different** user's data if the caller's token carries an elevated permission claim (e.g. `AIM:UserAdmin:R`) — otherwise return `403 Forbidden`. This matters most for `getEmailOfUser` (§2.5), which returns PII.
- **Response shape:** wrap in the existing `Result<T>` → `ProblemDetails` convention from `.claude` §6. No stack traces or SQL text in error bodies.
- **Caching:** Module/Function/Role mapping data changes rarely; consider a short server-side Redis cache (e.g. `ubis:menu:{roleId}:{financialYear}`, 5–10 minute TTL, invalidated on any RBAC mapping edit) to avoid re-querying `M_RoleModuleMapping`/`M_RoleFunctionMapping` on every menu render. Optional for v1, but call it out in code comments as a follow-up.

### 2.1 `GET /api/menu/modules?financialYear={fy}` — ModuleListForUserId
- Resolves caller's `UserId` from JWT (see IDOR guard above).
- Look up the user's active `Role` for the given `financialYear` via `aim.UserCharges` (existing entity from `.claude` §2), falling back to the user's default `Role` if no `financialYear` supplied or no active charge found for it.
- Query `M_RoleModuleMapping` where `RoleId = <resolved role>` and `Active = 1` and `RMFreez = 0`, join `M_Module` where `Active = 1`, order by `Display_SequenceNo`.
- Response:
```json
{
  "modules": [
    { "moduleId": "...", "moduleName": "Budget Allocation", "sequenceNo": 1 },
    { "moduleId": "...", "moduleName": "Fund Release", "sequenceNo": 2 }
  ]
}
```

### 2.2 `GET /api/menu/functions?moduleName={name}&financialYear={fy}` — FunctionNamesForUserIdAndModuleName
- Same role-resolution as §2.1.
- Resolve `ModuleId` from `M_Module` by `ModuleName` (exact, case-insensitive match); `404` if not found or not active.
- Query `M_RoleFunctionMapping` where `RoleId = <resolved role>` and `ModuleID = <resolved module>` and `Active = 1` and `RFFreez = 0`, join `M_Function` where `Active = 1` and `Freez = 0`, order by `RF_SequenceNo`.
- Response:
```json
{
  "moduleName": "Budget Allocation",
  "functions": [
    { "functionId": "...", "functionName": "View Allocation", "sequenceNo": 1 },
    { "functionId": "...", "functionName": "Approve Allocation", "sequenceNo": 2 }
  ]
}
```
- Note: this is the read-only "what's on the menu" list. It is deliberately separate from the CRUD-permission matrix (`C/R/U/D` flags) already built into the JWT claims per `.claude` §3 step 5 — a Function can appear on the menu without every CRUD flag being true; the menu just needs the name/order, the token already carries the fine-grained rights.

### 2.3 `GET /api/menu/full?financialYear={fy}` — RetrieveMenuForUserId
- Combines §2.1 and §2.2 into one nested call so the MVC layout/navigation partial can render the whole sidebar in a single round trip instead of one call per module.
- Response:
```json
{
  "menu": [
    {
      "moduleId": "...",
      "moduleName": "Budget Allocation",
      "sequenceNo": 1,
      "functions": [
        { "functionId": "...", "functionName": "View Allocation", "sequenceNo": 1 },
        { "functionId": "...", "functionName": "Approve Allocation", "sequenceNo": 2 }
      ]
    },
    {
      "moduleId": "...",
      "moduleName": "Fund Release",
      "sequenceNo": 2,
      "functions": [ ... ]
    }
  ]
}
```
- Implementation note: build this from a single joined query (`M_RoleModuleMapping` ⋈ `M_Module` ⋈ `M_RoleFunctionMapping` ⋈ `M_Function`, all filtered by the resolved `RoleId`) rather than calling §2.1/§2.2 internally in a loop — avoid N+1 queries.

### 2.4 `GET /api/users/role?userId={id}&financialYear={fy}` — getRoleOfUser
- `userId` optional — omit to mean "me" (from JWT); supplying someone else's `userId` requires the elevated permission claim (see general IDOR rule).
- Returns the role for the given/implied `financialYear` (or the default/current one if omitted), resolved the same way as §2.1.
- Response: `{ "userId": "...", "roleId": "...", "roleName": "Budget Officer", "financialYear": "2026-27" }`.

### 2.5 `GET /api/users/email?userId={id}` — getEmailOfUser
- Same optional `userId` + elevated-permission rule as §2.4, but treat this one as **higher sensitivity** than the others (it's the field the OTP flows in Addendum 01 depend on):
  - Only "self" lookups or callers with an explicit `AIM:UserAdmin:R` (or a dedicated `AIM:System:EmailLookup` service-to-service claim, see §3) may call this.
  - Log every non-self call to `aim.SecurityEvents` as `EventType = "EmailLookupByAdmin"` with the caller's `UserId` and the target `UserId` — this one is sensitive enough to always audit, unlike the module/function reads.
  - Response: `{ "userId": "...", "email": "us***@domain.com" }` for anything except a genuine self-lookup or a first-party server-to-server call (e.g. EmailService's own internal lookup, if it ever needs to re-resolve an email — normally it won't, since AIM already passes the email in the OTP message per Addendum 01 §1.1) — mask the email in the response for admin-initiated lookups, return unmasked only for self.

---

## 3. Service-to-Service Caller Restriction (network/config layer, separate from JWT user auth)

This is a **second, independent security layer**: JWT bearer auth (already built) proves *which user* is calling; this layer restricts *which service/origin* is even allowed to reach a microservice's endpoints at all — i.e., every microservice (AIM, EmailService, any future ones) should, in non-dev environments, only accept calls that originate from the MVC application server, not be reachable directly from arbitrary clients.

### 3.1 Settings file changes (`appsettings.json` in every microservice, plus environment-specific overrides)

```json
{
  "CallerRestriction": {
    "Enabled": true,
    "AllowedCallerBaseUrls": [
      "https://ubis-mvc.gov.in",
      "https://ubis-mvc-staging.internal.gov.in"
    ],
    "AllowedCallerHostHeaderValues": [
      "ubis-mvc.gov.in",
      "ubis-mvc-staging.internal.gov.in"
    ],
    "SharedClientKeyHeaderName": "X-UBIS-Internal-Client-Key",
    "SharedClientKey": "<set via environment variable / secret store, never checked into appsettings.json in plaintext>"
  }
}
```

- `appsettings.Development.json` overrides this with **`"Enabled": false`** — this is the "switch" the developers use locally. It is an environment-specific file, so a developer never has to remember to flip anything by hand when they run `dotnet run` under the `Development` environment; it's off by default there and on by default everywhere else.
- `appsettings.Testing.json` (or whatever the unit/integration test host uses) similarly sets `"Enabled": false`, so `WebApplicationFactory<T>`-based integration tests don't need to fake headers.
- **Do not** rely on `ASPNETCORE_ENVIRONMENT == "Development"` alone as the switch condition in code — read the explicit `CallerRestriction:Enabled` flag instead, and let the per-environment `appsettings.*.json` files set that flag's value. This way a developer can still explicitly force it on in Development to test the restriction logic itself, without touching code.

### 3.2 Middleware (add to every microservice's `Program.cs`, right after `UseAuthentication()`/before `UseAuthorization()` or as its own small middleware — implementation, not code-complete)

```csharp
// Core/Shared/UBIS.Shared.Security/CallerRestrictionMiddleware.cs
public class CallerRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CallerRestrictionOptions _options; // bound from "CallerRestriction" section

    public CallerRestrictionMiddleware(RequestDelegate next, IOptions<CallerRestrictionOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context); // dev/test switch — restriction fully bypassed
            return;
        }

        // 1. Shared-key check (primary control — Origin/Referer headers are client-suppliable and
        //    not sent at all on plain server-to-server calls, so they're a weak signal alone).
        if (!context.Request.Headers.TryGetValue(_options.SharedClientKeyHeaderName, out var providedKey)
            || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedKey.ToString()),
                    Encoding.UTF8.GetBytes(_options.SharedClientKey)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Caller not authorized." });
            return;
        }

        // 2. Optional secondary check: Host header of the incoming request's declared origin,
        //    if the MVC app forwards one (defense-in-depth, not a substitute for the key check above).
        var declaredOrigin = context.Request.Headers["X-UBIS-Caller-Host"].ToString();
        if (!string.IsNullOrEmpty(declaredOrigin)
            && !_options.AllowedCallerHostHeaderValues.Contains(declaredOrigin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Caller host not recognized." });
            return;
        }

        await _next(context);
    }
}
```
- Register in every microservice: `app.UseMiddleware<CallerRestrictionMiddleware>();`.
- The MVC application's own outbound `HttpClient`(s) to each microservice must be configured (via `IHttpClientFactory` named clients, in the MVC project's own `appsettings.json`) to always attach `X-UBIS-Internal-Client-Key` (and optionally `X-UBIS-Caller-Host`) — this is the counterpart config on the calling side, not just the receiving side.
- **Why a shared key rather than only an IP/URL allowlist:** in a load-balanced/Kubernetes (AKS) environment, the "allowed URL" of the MVC project is a logical hostname behind a service mesh/ingress, not a fixed IP, and Origin/Referer headers aren't reliably sent on server-to-server HTTP calls at all — so the shared-key header is the actual enforceable control; the URL/host list is a secondary, defense-in-depth check, not the primary one. If the team wants stronger-than-shared-key isolation later, mutual TLS (client certificates) between the MVC host and each microservice is the natural upgrade path — flag this as a future hardening item, not required for v1.

### 3.3 Where this sits relative to JWT auth
Both layers apply together on every one of the endpoints in §1: `CallerRestrictionMiddleware` runs first (rejects non-MVC callers outright, cheaply, before any auth/DB work), then the existing JWT bearer authentication/authorization from `.claude` §3/§6 runs as before, unchanged.

---

## 4. Implementation Instruction to Claude Code

> Extend the existing solution — do not regenerate it. Add `MenuController` and `UserLookupController` to `AIM.WebApi/Controllers/` implementing the five endpoints in §2, reusing the existing `AimDbContext`, `Result<T>` pattern, and JWT claims-resolution helper already present from `.claude`. Add a new shared project (or a folder in an existing shared/common project if one already exists in the solution) `UBIS.Shared.Security` containing `CallerRestrictionMiddleware` and `CallerRestrictionOptions` per §3.2, and wire it into `Program.cs` of **every** microservice currently in the solution (AIM and EmailService, plus any others already generated) — not just AIM. Add the `CallerRestriction` settings block from §3.1 to each microservice's `appsettings.json`, and add `appsettings.Development.json` / `appsettings.Testing.json` overrides setting `Enabled: false` for each. Update the MVC project's own `appsettings.json` and typed `HttpClient` registrations to attach the shared key header on every outbound call to AIM/EmailService/etc. Do not modify any endpoints or files already delivered under Addendum 01 except where §1 of this document explicitly says an existing route is reused as-is.
