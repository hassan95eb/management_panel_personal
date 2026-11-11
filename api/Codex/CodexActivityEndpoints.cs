using System.Security.Cryptography;
using System.Text;
using ManagementPanel.Api.Activities;
using ManagementPanel.Api.Areas;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Codex;

public static class CodexActivityEndpoints
{
    public static void MapCodexActivityEndpoints(this WebApplication app)
    {
        app.MapPost("/api/codex/projects/{projectId:guid}/activities", RecordAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> RecordAsync(
        Guid projectId, ActivityInput input, HttpRequest request, PanelDbContext db, CancellationToken ct)
    {
        var hash = CodexBearerKey.ReadHash(request);
        if (hash is null) return Results.Unauthorized();

        var report = input.Report?.Trim() ?? "";
        var link = string.IsNullOrWhiteSpace(input.Link) ? null : input.Link.Trim();
        if (!Guid.TryParseExact(request.Headers["Idempotency-Key"].ToString(), "D", out var operationId) ||
            operationId == Guid.Empty || report.Length is 0 or > 4000 || !ProjectActivityEndpoints.IsWebUrl(link))
            return Results.BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize writes per key and coordinate with the UPDATE that revokes it.
        var keyIds = await db.Database.SqlQuery<Guid>(
            $"SELECT \"Id\" AS \"Value\" FROM codex_keys WHERE \"SecretHash\" = {hash} AND \"RevokedAt\" IS NULL FOR UPDATE")
            .ToListAsync(ct);
        if (keyIds.Count == 0) return Results.Unauthorized();

        var keyId = keyIds[0];
        var projectIds = await db.Database.SqlQuery<Guid>(
            $"""
            SELECT project."Id" AS "Value"
            FROM projects AS project
            JOIN codex_project_grants AS grant_row ON grant_row."ProjectId" = project."Id"
            WHERE project."Id" = {projectId} AND project."AreaKey" = {Area.ProjectsKey}
                AND project."ArchivedAt" IS NULL AND grant_row."KeyId" = {keyId}
                AND grant_row."CanWriteActivity"
            FOR SHARE OF project
            """).ToListAsync(ct);
        if (projectIds.Count == 0) return Results.NotFound();

        var activityId = GetActivityId(keyId, operationId);
        var actorId = $"codex:{keyId:D}";
        var existing = await db.ProjectActivities.AsNoTracking()
            .SingleOrDefaultAsync(activity => activity.Id == activityId, ct);
        if (existing is not null)
        {
            return existing.Source == "codex" && existing.ActorId == actorId &&
                existing.ProjectId == projectId && existing.TaskId == input.TaskId &&
                existing.Report == report && existing.Link == link
                ? Results.Ok(existing)
                : Results.Conflict();
        }

        if (input.TaskId is Guid taskId &&
            !await db.ProjectTasks.AnyAsync(task => task.Id == taskId && task.ProjectId == projectId, ct))
            return Results.BadRequest();

        var activity = new ProjectActivity
        {
            Id = activityId,
            ProjectId = projectId,
            TaskId = input.TaskId,
            Report = report,
            Link = link,
            ActorId = actorId,
            Source = "codex",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProjectActivities.Add(activity);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Json(activity, statusCode: StatusCodes.Status201Created);
    }

    private static Guid GetActivityId(Guid keyId, Guid operationId)
    {
        // Derive a version 8 UUID so retries resolve to the same persisted activity.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"panel:codex:activity:{keyId:D}:{operationId:D}"));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    private sealed record ActivityInput(string? Report, string? Link, Guid? TaskId);
}
