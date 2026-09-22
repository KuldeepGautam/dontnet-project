using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using UBIS.Web.Configuration;
using UBIS.Web.Services.Caching;
using UBIS.Web.Services.Captcha;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Configuration;
using UBIS.Web.Services.Logging;
using UBIS.Web.Services.Session;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configuration binding
// ============================================================================
builder.Services.Configure<MicroserviceUrlsOptions>(builder.Configuration.GetSection("MicroserviceUrls"));
builder.Services.Configure<InternalCallerOptions>(builder.Configuration.GetSection("InternalCaller"));
builder.Services.Configure<AppSessionOptions>(builder.Configuration.GetSection("SessionOptions"));
builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection("AuthCookie"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<AdminRoleOptions>(builder.Configuration.GetSection("AdminRoles"));

var microserviceUrls = builder.Configuration.GetSection("MicroserviceUrls").Get<MicroserviceUrlsOptions>()
    ?? new MicroserviceUrlsOptions();
var sessionOptions = builder.Configuration.GetSection("SessionOptions").Get<AppSessionOptions>()
    ?? new AppSessionOptions();
var authCookieOptions = builder.Configuration.GetSection("AuthCookie").Get<AuthCookieOptions>()
    ?? new AuthCookieOptions();

// Offline-intranet default: HTTP only. Set dbo.AppSettings.EnableHttps = 1 for a TLS-terminated
// deployment; never enable this for anything internet-facing without a full security review.
// This value is needed before builder.Build() (it configures the Session/Auth cookies' SecurePolicy
// below, which is a service-registration-time decision), so - unlike every other service, which can
// resolve its DB-backed AppSettingsReader from DI after Build() - this has to be a raw, un-DI'd HTTP
// call to AIM's api/app-settings endpoint (UBIS_Web has no direct DB access of its own). Falls back
// to false (fail-safe to the same offline-intranet default dbo.AppSettings.EnableHttps itself
// defaults to) if AIM isn't reachable yet at startup (a real scenario in dev when this app is
// brought up independently of AIM) rather than failing startup outright.
var useHttps = GetEnableHttpsAtStartup(microserviceUrls.Aim);

static bool GetEnableHttpsAtStartup(string aimBaseUrl)
{
    const bool fallback = false;
    try
    {
        using var http = new HttpClient { BaseAddress = new Uri(aimBaseUrl), Timeout = TimeSpan.FromSeconds(5) };
        using var response = http.GetAsync("api/app-settings").GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            return fallback;
        }

        var snapshot = response.Content.ReadFromJsonAsync<UBIS.Web.Services.Clients.AppSettingsSnapshotDto>().GetAwaiter().GetResult();
        return snapshot?.EnableHttps ?? fallback;
    }
    catch
    {
        // AIM unreachable at startup - fall back rather than crash UBIS_Web's own startup on it.
        return fallback;
    }
}

// Security-team requirement: every cookie this app sets must be scoped under /UBIS, not the site
// root "/" — see Security:CookiePath in appsettings.json for the full rationale. Applied below to
// the Session, Auth, and Antiforgery cookies (every cookie this app issues).
var cookiePath = builder.Configuration["Security:CookiePath"] ?? "/";

// "10 max retries then break the circuit" — raised from 5 (client testing feedback, 2026-08-24:
// "AIM service unreachable"/"Pre Budget Service temporarily unavailable" under concurrent load).
// See NFR_ARCHITECTURE.md. Configurable so ops can tune it per environment without a rebuild.
var maxRetryAttempts = builder.Configuration.GetValue<int?>("Resilience:MaxRetryAttempts") ?? 10;

// ============================================================================
// MVC + global silent-refresh filter
// ============================================================================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<TokenRefreshFilter>();
    options.Filters.Add<ComplianceInterceptorFilter>();
    options.Filters.Add<BreadcrumbFilter>();
});

// ============================================================================
// Redis: distributed cache backing ASP.NET Core Session (JWT/refresh/permissions/menu
// live here server-side; the browser only ever sees an opaque session cookie) AND the
// L2 tier of IAppCacheService (per-role menu cache, data-entry drafts). IMemoryCache is
// the L1 tier — see AppCacheService for why a hand-rolled two-tier cache is used here
// instead of HybridCache (its GetOrCreateAsync-only surface doesn't fit plain
// read/write/remove access patterns without introducing incorrect negative caching).
// ============================================================================
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAppCacheService, AppCacheService>();

// Login CAPTCHA (added 2026-07) — stateless server-side service, no external dependency.
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = sessionOptions.CookieName;
    options.IdleTimeout = TimeSpan.FromMinutes(sessionOptions.IdleTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = useHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    // Host-only: never widen this to a parent domain. Each IIS-hosted service in this
    // solution is a separate, independently-pooled app — cookies must not leak between them.
    options.Cookie.Domain = null;
    options.Cookie.Path = cookiePath;
});

// ============================================================================
// Cookie authentication: the cookie carries only an opaque identity, never the JWT.
// ============================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authCookieOptions.Name;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = useHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        // Host-only — see the Session cookie comment above; same rationale applies here.
        options.Cookie.Domain = null;
        options.Cookie.Path = cookiePath;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(authCookieOptions.ExpireMinutes);
        options.SlidingExpiration = true;
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";
        options.AccessDeniedPath = "/User/Login";
    });
builder.Services.AddAuthorization();

// AJAX antiforgery for the autosave draft API (wwwroot/js/autosave.js sends this header
// instead of a form field, since it posts JSON, not a form submission).
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Path = cookiePath;
});

// Data Protection keys persisted to disk so auth cookies survive IIS app-pool recycles.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")))
    .SetApplicationName("UBIS_Web");

// ============================================================================
// Defense-in-depth rate limiting on the login endpoint (AIM already rate-limits
// per-username; this caps requests per client IP before they even reach AIM).
// ============================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// ============================================================================
// Typed HttpClients calling AIM, MenuGenerator, PreBudget, etc., all routed through the
// internal-caller handler (shared-secret header) and an explicit resilience pipeline:
// up to 10 retries (exponential backoff, jitter, capped MaxDelay so a long retry run stays
// bounded), then the circuit breaker opens and callers get a graceful failure —
// AimClient/MenuClient/PreBudgetClient/etc. log the final give-up to LogWriter and return
// ApiCallResult.Failure instead of throwing. Sized generously for the ~1000 concurrent user
// capacity target (see Capacity:TargetConcurrentUsers / /healthz).
//
// Raised 2026-08-24 (client testing feedback: "AIM service unreachable" / "Pre Budget Service
// temporarily unavailable" even under modest concurrent load, reproduced on the local box too).
// The old defaults (MinimumThroughput=8, 30s sampling, 10s attempt timeout) meant a handful of
// individually-slow requests from just a few concurrent users was enough to trip the circuit for
// EVERYONE for the next 30s, turning transient slowness into a synthetic outage. Every client now
// gets the same more lenient shape PreBudget was given first: longer per-attempt timeout, a wider
// sampling window with a higher throughput floor before the breaker can act at all, and a higher
// failure ratio required to open it. PreBudget keeps its own even-more-lenient pipeline on top of
// this (its appendix queries remain the heaviest), but the underlying resilience-amplification
// pattern that caused AIM's failures is now fixed everywhere it was previously repeated.
// ============================================================================
builder.Services.AddTransient<InternalCallerHandler>();

void ConfigureResilience(HttpStandardResilienceOptions options)
{
    options.Retry.MaxRetryAttempts = maxRetryAttempts;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.MaxDelay = TimeSpan.FromSeconds(5);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(45);
    options.CircuitBreaker.FailureRatio = 0.7;
    options.CircuitBreaker.MinimumThroughput = 20;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
}

// PreBudget gets its own, more lenient pipeline (2026-08-24 - "10 users testing, failing for
// 50%, Pre Budget Service temporarily unavailable"): with MinimumThroughput=8 and a 30s sampling
// window, a page's worth of appendix calls from just a handful of concurrent users is enough to
// hit 8 requests; if even 4 of those are individually slow (PreBudget's queries are heavier than
// AIM/MenuGenerator's - joins/aggregations across the appendix tables) the circuit trips, and
// EVERY user's request - including ones that would have succeeded - gets an immediate synthetic
// 503 for the next 30s. That amplifies a handful of slow requests into a widespread outage. Raises
// MinimumThroughput so the circuit needs real, sustained evidence of failure before it opens, and
// gives each individual attempt more time (AttemptTimeout) before it's counted as a failure at all.
void ConfigurePreBudgetResilience(HttpStandardResilienceOptions options)
{
    options.Retry.MaxRetryAttempts = maxRetryAttempts;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.MaxDelay = TimeSpan.FromSeconds(8);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(40);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
    options.CircuitBreaker.FailureRatio = 0.7;
    options.CircuitBreaker.MinimumThroughput = 30;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
}

// ECL gets the same lenient pipeline as PreBudget above (bug report 2026-08-27: "Could not load
// rows." on ECL/ECLApprove/DataToApprove) - ECLApproveController.BuildGridAsync calls
// GetOutlaysAsync with demandId=null whenever "Demand: All" is selected, which - confirmed via a
// direct call against the ECL WebApi - returns every outlay row across every demand the caller can
// see (860KB+ of JSON for a broadly-permissioned role), the same "heavier, join-resolved query"
// shape that caused PreBudget's identical symptom. Reusing the generic 15s-per-attempt pipeline
// here left this exact page one slow moment away from timing out.
void ConfigureEclResilience(HttpStandardResilienceOptions options)
{
    options.Retry.MaxRetryAttempts = maxRetryAttempts;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.MaxDelay = TimeSpan.FromSeconds(8);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(40);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
    options.CircuitBreaker.FailureRatio = 0.7;
    options.CircuitBreaker.MinimumThroughput = 30;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
}

builder.Services.AddHttpClient<IAimClient, AimClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.Aim);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

// Cached wrapper around IAimClient.GetAppSettingsAsync() - see AppSettingsCache's own doc comment.
builder.Services.AddSingleton<AppSettingsCache>();

builder.Services.AddHttpClient<IMenuClient, MenuClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.MenuGenerator);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

builder.Services.AddHttpClient<IUserProfileClient, UserProfileClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.UserProfile);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

builder.Services.AddHttpClient<IPreBudgetClient, PreBudgetClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.PreBudget);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigurePreBudgetResilience);

builder.Services.AddHttpClient<IEclClient, EclClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.Ecl);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureEclResilience);

builder.Services.AddHttpClient<IReportingClient, ReportingClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.Reporting);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

builder.Services.AddHttpClient<IEclClient, EclClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.Ecl);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

builder.Services.AddHttpClient<IReportingClient, ReportingClient>(client =>
{
    client.BaseAddress = new Uri(microserviceUrls.Reporting);
    client.DefaultRequestVersion = HttpVersion.Version11;
})
    .AddHttpMessageHandler<InternalCallerHandler>()
    .AddStandardResilienceHandler(ConfigureResilience);

// ============================================================================
// Centralized logging via RabbitMQ (same exchange AIM/MenuGenerator publish to).
// ============================================================================
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<IWebLogClient, RabbitMqWebLogClient>();

var app = builder.Build();

// ============================================================================
// HTTP pipeline
// ============================================================================

// Swallows the benign TaskCanceledException/OperationCanceledException that fires whenever a
// client disconnects mid-request (most commonly: the user hits refresh/navigates away while a
// page is still loading, which cancels the in-flight request's HttpContext.RequestAborted token -
// any await downstream that observes that token, directly or via an HttpClient call, throws this).
// There's no client left to see a response either way, so this just stops processing quietly
// instead of letting it fall through to the exception handler below and get shown to the user (in
// Development, with no UseDeveloperExceptionPage registered before this fix, it rendered as a raw
// unhandled-exception dump - "System.Threading.Tasks.TaskCanceledException: 'A task was
// canceled.'" - on every fast manual refresh). Registered first so it wraps the entire pipeline.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // Client already gone - nothing to write back, nothing actionable to log as an error.
    }
});

// Must match Security:CookiePath exactly: a cookie's Path is a request-URL prefix match, so
// scoping the Session/Auth/Antiforgery cookies under /UBIS only works if the app's own routes
// actually live under /UBIS too (e.g. /UBIS/User/Login) — otherwise the browser would never send
// the cookie back on any real request. This does not change IIS hosting topology (still one
// dedicated site/pool per PUBLISH_TO_IIS_VS2026_GUIDE.md) — it only prefixes this app's own
// routes/link-generation/redirects, which ASP.NET Core applies automatically everywhere
// (asp-controller/asp-action tag helpers, LoginPath/LogoutPath, RedirectToAction, etc.).
if (cookiePath != "/")
{
    app.UsePathBase(cookiePath);
}

if (app.Environment.IsDevelopment())
{
    // Was entirely missing before this fix - Development had NO exception-handling middleware at
    // all (UseExceptionHandler below is Production-only), so any *other* genuine unhandled
    // exception during development also rendered as a raw dump instead of the standard ASP.NET
    // Core developer exception page with the actual stack trace.
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/User/Error");
}

if (useHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    // Development-only: Visual Studio injects Browser Link and the dotnet-watch hot-reload
    // script into every response, both of which open a websocket/HTTP connection to a random
    // localhost port chosen at launch (not knowable in advance, so this can't be a fixed
    // allow-list entry). Without this, those two dev-tooling connections are blocked by
    // connect-src falling back to default-src 'self' — harmless (the app itself still works
    // fully over 'self'), but it spams the console on every page load while debugging. Scoped to
    // Development only; Production keeps the exact same strict policy as before (no connect-src
    // override, i.e. same-origin only) per NFR_ARCHITECTURE.md §10's offline-intranet posture.
    var connectSrc = app.Environment.IsDevelopment() ? " connect-src 'self' http://localhost:* ws://localhost:*;" : string.Empty;
    context.Response.Headers.Append(
        "Content-Security-Policy",
        $"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none';{connectSrc}");

    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
    }

    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Lightweight liveness/config probe — useful for a load balancer health check and for
// confirming the engineered capacity target at a glance (see Capacity:TargetConcurrentUsers
// in appsettings.json and NFR_ARCHITECTURE.md for how 1000 concurrent users is achieved).
app.MapGet("/healthz", (IConfiguration config) => Results.Ok(new
{
    status = "healthy",
    service = "UBIS_Web",
    targetConcurrentUsers = config.GetValue<int?>("Capacity:TargetConcurrentUsers") ?? 1000,
    useHttps
})).AllowAnonymous();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();
