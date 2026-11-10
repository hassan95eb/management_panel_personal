using System.Security.Cryptography;
using System.Text;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Codex;

public static class CodexKeyEndpoints
{
    public static void MapCodexKeyEndpoints(this WebApplication app)
    {
        var keys = app.MapGroup("/api/codex-keys").RequireAuthorization(policy => policy.RequireRole("Owner"));

        keys.MapGet("", async (PanelDbContext db, CancellationToken ct) =>
        {
            var items = await db.CodexKeys.AsNoTracking()
                .OrderByDescending(key => key.CreatedAt).ThenBy(key => key.Id)
                .Select(key => new { key.Id, key.Name, key.CreatedAt, key.RevokedAt, key.Grants })
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        keys.MapPost("", async (KeyInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (context.Request.Headers["X-Panel-Request"] != "1") return Results.BadRequest();
            var name = input.Name?.Trim() ?? "";
            var grants = input.Grants;
            if (name.Length is 0 or > 100 || grants is null || grants.Length is 0 or > 100 ||
                grants.Any(grant => grant is null || (!grant.CanRead && !grant.CanWriteActivity)))
                return Results.BadRequest();

            var projectIds = grants.Select(grant => grant.ProjectId).Distinct().ToArray();
            if (projectIds.Length != grants.Length ||
                await db.Projects.CountAsync(project => projectIds.Contains(project.Id) && project.ArchivedAt == null, ct) != projectIds.Length)
                return Results.BadRequest();

            var id = Guid.NewGuid();
            var token = "mpk_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var key = new CodexKey
            {
                Id = id,
                Name = name,
                SecretHash = HashToken(token),
                CreatedAt = DateTimeOffset.UtcNow,
                Grants = grants.Select(grant => new CodexProjectGrant
                {
                    KeyId = id,
                    ProjectId = grant.ProjectId,
                    CanRead = grant.CanRead,
                    CanWriteActivity = grant.CanWriteActivity
                }).ToList()
            };

            db.CodexKeys.Add(key);
            await db.SaveChangesAsync(ct);
            return Results.Json(new { key.Id, key.Name, key.CreatedAt, key.RevokedAt, key.Grants, Token = token },
                statusCode: StatusCodes.Status201Created);
        });

        keys.MapDelete("/{id:guid}", async (Guid id, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (context.Request.Headers["X-Panel-Request"] != "1") return Results.BadRequest();
            var revokedAt = DateTimeOffset.UtcNow;
            var updated = await db.CodexKeys.Where(key => key.Id == id && key.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(key => key.RevokedAt, revokedAt), ct);
            return updated > 0 || await db.CodexKeys.AnyAsync(key => key.Id == id, ct)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed record KeyInput(string? Name, GrantInput[]? Grants);
    private sealed record GrantInput(Guid ProjectId, bool CanRead, bool CanWriteActivity);
}
