using ManagementPanel.Api.Codex;
using ManagementPanel.Api.Dashboard;
using ManagementPanel.Api.Activities;
using ManagementPanel.Api.Projects;
using ManagementPanel.Api.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PanelDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Panel");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Set ConnectionStrings__Panel to use the PostgreSQL database.");
    }

    options.UseNpgsql(connectionString);
});

var ownerPassword = builder.Configuration["Owner:Password"];
var ownerPasswordHash = string.IsNullOrWhiteSpace(ownerPassword) || ownerPassword.Length < 16
    ? null
    : HashPassword(ownerPassword);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Environment.IsDevelopment()
            ? "management-panel-session"
            : "__Host-management-panel-session";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireRole("Owner").Build());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("owner-login", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }

    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapPost("/api/auth/login", async (HttpContext context, OwnerLogin login) =>
{
    if (!IsPanelRequest(context)) return Results.BadRequest();
    if (ownerPasswordHash is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

    if (login.Password is null ||
        !CryptographicOperations.FixedTimeEquals(HashPassword(login.Password), ownerPasswordHash))
    {
        return Results.Unauthorized();
    }

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "owner"), new Claim(ClaimTypes.Role, "Owner")],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity));
    return Results.NoContent();
}).AllowAnonymous().RequireRateLimiting("owner-login");

app.MapGet("/api/auth/session", () => Results.Ok(new { role = "owner" }));

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    if (!IsPanelRequest(context)) return Results.BadRequest();

    await context.SignOutAsync();
    return Results.NoContent();
});

app.MapProjectEndpoints();
app.MapProjectTaskEndpoints();
app.MapProjectActivityEndpoints();
app.MapDashboardEndpoints();
app.MapCodexKeyEndpoints();

app.Run();

static byte[] HashPassword(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

static bool IsPanelRequest(HttpContext context) =>
    context.Request.Headers["X-Panel-Request"] == "1";

record OwnerLogin(string? Password);
