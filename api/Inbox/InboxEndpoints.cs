using ManagementPanel.Api.Data;
using ManagementPanel.Api.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Inbox;

public static class InboxEndpoints
{
    public static void MapInboxEndpoints(this WebApplication app)
    {
        var inbox = app.MapGroup("/api/inbox").RequireAuthorization(policy => policy.RequireRole("Owner"));
        inbox.MapGet("", async (PanelDbContext db, CancellationToken ct) =>
            Results.Ok(await db.InboxItems.AsNoTracking().OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id).ToListAsync(ct)));

        inbox.MapPost("", async (CaptureInput input, HttpRequest request, PanelDbContext db, CancellationToken ct) =>
        {
            var text = input.Text?.Trim() ?? "";
            if (request.Headers["X-Panel-Request"] != "1" || text.Length is 0 or > 4000)
                return Results.BadRequest();

            var item = new InboxItem { Id = Guid.NewGuid(), Text = text, CreatedAt = DateTimeOffset.UtcNow };
            db.InboxItems.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Json(item, statusCode: StatusCodes.Status201Created);
        });

        inbox.MapPut("/{id:guid}/project", AssignAsync);
    }

    private static async Task<IResult> AssignAsync(
        Guid id, AssignmentInput input, HttpRequest request, PanelDbContext db, CancellationToken ct)
    {
        var title = input.TaskTitle?.Trim();
        if (request.Headers["X-Panel-Request"] != "1" || input.ProjectId == Guid.Empty ||
            title is { Length: 0 or > 200 })
            return Results.BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var items = await db.InboxItems.FromSqlInterpolated(
            $"SELECT * FROM inbox_items WHERE \"Id\" = {id} FOR UPDATE").ToListAsync(ct);
        var item = items.SingleOrDefault();
        if (item is null) return Results.NotFound();

        if (item.TaskId is Guid existingTaskId)
        {
            if (item.ProjectId != input.ProjectId) return Results.Conflict();
            if (title is null) return Results.Ok(item);

            var existingTitle = await db.ProjectTasks.Where(task => task.Id == existingTaskId)
                .Select(task => task.Title).SingleAsync(ct);
            return existingTitle == title ? Results.Ok(item) : Results.Conflict();
        }

        var projects = await db.Database.SqlQuery<Guid>(
            $"SELECT \"Id\" AS \"Value\" FROM projects WHERE \"Id\" = {input.ProjectId} AND \"ArchivedAt\" IS NULL FOR SHARE")
            .ToListAsync(ct);
        if (projects.Count == 0) return Results.NotFound();

        item.ProjectId = input.ProjectId;
        if (title is not null)
        {
            var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = input.ProjectId, Title = title };
            db.ProjectTasks.Add(task);
            item.TaskId = task.Id;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(item);
    }

    private sealed record CaptureInput(string? Text);
    private sealed record AssignmentInput(Guid ProjectId, string? TaskTitle);
}
