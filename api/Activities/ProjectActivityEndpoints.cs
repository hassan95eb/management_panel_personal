using System.Security.Claims;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Activities;

public static class ProjectActivityEndpoints
{
    public static void MapProjectActivityEndpoints(this WebApplication app)
    {
        app.MapPost("/api/projects/{projectId:guid}/activities", async (
            Guid projectId, ActivityInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (context.Request.Headers["X-Panel-Request"] != "1") return Results.BadRequest();

            var actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(actorId)) return Results.Forbid();

            var report = input.Report?.Trim() ?? "";
            var link = string.IsNullOrWhiteSpace(input.Link) ? null : input.Link.Trim();
            if (report.Length is 0 or > 4000 || !IsWebUrl(link)) return Results.BadRequest();

            var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId, ct);
            if (project is null) return Results.NotFound();
            if (project.ArchivedAt is not null) return Results.Conflict();
            if (input.TaskId is Guid taskId &&
                !await db.ProjectTasks.AnyAsync(task => task.Id == taskId && task.ProjectId == projectId, ct))
                return Results.BadRequest();

            var activity = new ProjectActivity
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                TaskId = input.TaskId,
                Report = report,
                Link = link,
                ActorId = actorId,
                Source = "manual",
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.ProjectActivities.Add(activity);
            await db.SaveChangesAsync(ct);
            return Results.Json(activity, statusCode: StatusCodes.Status201Created);
        }).RequireAuthorization(policy => policy.RequireRole("Owner"));
    }

    private static bool IsWebUrl(string? link) =>
        link is null || (link.Length <= 2048 &&
            Uri.TryCreate(link, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host));

    private sealed record ActivityInput(string? Report, string? Link, Guid? TaskId);
}
