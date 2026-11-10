using System.Security.Claims;
using ManagementPanel.Api.Activities;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Tasks;

public static class ProjectTaskEndpoints
{
    public static void MapProjectTaskEndpoints(this WebApplication app)
    {
        var tasks = app.MapGroup("/api/projects/{projectId:guid}/tasks")
            .RequireAuthorization(policy => policy.RequireRole("Owner"));

        tasks.MapGet("", async (Guid projectId, PanelDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(project => project.Id == projectId, ct))
                return Results.NotFound();

            var items = await db.ProjectTasks.AsNoTracking()
                .Where(task => task.ProjectId == projectId)
                .OrderBy(task => task.DueDate == null)
                .ThenBy(task => task.DueDate)
                .ThenBy(task => task.Title)
                .ThenBy(task => task.Id)
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        tasks.MapGet("/{id:guid}", async (Guid projectId, Guid id, PanelDbContext db, CancellationToken ct) =>
        {
            var task = await db.ProjectTasks.AsNoTracking()
                .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == id, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        tasks.MapPost("", async (Guid projectId, TaskInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (!IsPanelRequest(context)) return Results.BadRequest();
            var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = projectId, Title = "" };
            if (!input.Apply(task)) return Results.BadRequest();

            var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId, ct);
            if (project is null) return Results.NotFound();
            if (project.ArchivedAt is not null) return Results.Conflict();

            db.ProjectTasks.Add(task);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/tasks/{task.Id}", task);
        });

        tasks.MapPut("/{id:guid}", async (Guid projectId, Guid id, TaskInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (!IsPanelRequest(context)) return Results.BadRequest();
            var task = new ProjectTask { Id = id, ProjectId = projectId, Title = "" };
            if (!input.Apply(task)) return Results.BadRequest();
            var actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(actorId)) return Results.Forbid();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            // Serialize edits so the previous status reflects the last committed change.
            var statuses = await db.Database.SqlQuery<string>(
                $"SELECT \"Status\" AS \"Value\" FROM project_tasks WHERE \"ProjectId\" = {projectId} AND \"Id\" = {id} FOR UPDATE")
                .ToListAsync(ct);
            var previousStatus = statuses.SingleOrDefault();
            if (previousStatus is null) return Results.NotFound();

            var scoped = db.ProjectTasks.Where(item => item.ProjectId == projectId && item.Id == id);
            var updated = await scoped
                .Where(_ => db.Projects.Any(project => project.Id == projectId && project.ArchivedAt == null))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Title, task.Title)
                    .SetProperty(item => item.Status, task.Status)
                    .SetProperty(item => item.Priority, task.Priority)
                    .SetProperty(item => item.DueDate, task.DueDate)
                    .SetProperty(item => item.BlockedReason, task.BlockedReason), ct);
            if (updated == 0) return Results.Conflict();

            if (previousStatus != task.Status)
            {
                db.ProjectActivities.Add(ProjectActivity.Automatic(
                    projectId, actorId, $"وضعیت تسک از {previousStatus} به {task.Status} تغییر کرد.", id));
                await db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return Results.Ok(task);
        });
    }

    private static bool IsPanelRequest(HttpContext context) =>
        context.Request.Headers["X-Panel-Request"] == "1";

    private sealed record TaskInput(string? Title, string? Status, string? Priority, DateOnly? DueDate, string? BlockedReason)
    {
        public bool Apply(ProjectTask task)
        {
            var title = Title?.Trim() ?? "";
            var status = Status ?? "todo";
            var priority = Priority ?? "medium";
            var reason = BlockedReason?.Trim() ?? "";
            if (title.Length is 0 or > 200 ||
                status is not ("todo" or "in-progress" or "blocked" or "done") ||
                priority is not ("low" or "medium" or "high") ||
                (status == "blocked" && reason.Length is 0 or > 1000))
                return false;

            task.Title = title;
            task.Status = status;
            task.Priority = priority;
            task.DueDate = DueDate;
            task.BlockedReason = status == "blocked" ? reason : null;
            return true;
        }
    }
}
