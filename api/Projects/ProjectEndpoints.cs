using System.Security.Claims;
using ManagementPanel.Api.Activities;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Projects;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var projects = app.MapGroup("/api/projects").RequireAuthorization(policy => policy.RequireRole("Owner"));

        projects.MapGet("", async (PanelDbContext db, CancellationToken ct) =>
            await db.Projects.AsNoTracking()
                .OrderBy(project => project.ArchivedAt != null)
                .ThenBy(project => project.Name)
                .ThenBy(project => project.Id)
                .ToListAsync(ct));

        projects.MapGet("/{id:guid}", async (Guid id, PanelDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
            return project is null ? Results.NotFound() : Results.Ok(project);
        });

        projects.MapPost("", async (ProjectInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (!IsPanelRequest(context)) return Results.BadRequest();
            var project = input.ToProject(Guid.NewGuid());
            if (!IsValid(project)) return Results.BadRequest();

            var actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(actorId)) return Results.Forbid();

            db.Projects.Add(project);
            db.ProjectActivities.Add(ProjectActivity.Automatic(project.Id, actorId, "پروژه ایجاد شد."));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{project.Id}", project);
        });

        projects.MapPut("/{id:guid}", async (Guid id, ProjectInput input, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (!IsPanelRequest(context)) return Results.BadRequest();
            var project = input.ToProject(id);
            if (!IsValid(project)) return Results.BadRequest();

            var updated = await db.Projects.Where(p => p.Id == id && p.ArchivedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Name, project.Name)
                    .SetProperty(p => p.Description, project.Description)
                    .SetProperty(p => p.Status, project.Status)
                    .SetProperty(p => p.RepositoryUrl, project.RepositoryUrl)
                    .SetProperty(p => p.EnvironmentUrl, project.EnvironmentUrl)
                    .SetProperty(p => p.NextStep, project.NextStep), ct);
            if (updated != 0) return Results.Ok(project);

            return await db.Projects.AnyAsync(p => p.Id == id, ct)
                ? Results.Conflict()
                : Results.NotFound();
        });

        projects.MapPut("/{id:guid}/archive", async (Guid id, HttpContext context, PanelDbContext db, CancellationToken ct) =>
        {
            if (!IsPanelRequest(context)) return Results.BadRequest();
            var actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(actorId)) return Results.Forbid();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var archivedAt = DateTimeOffset.UtcNow;
            var updated = await db.Projects.Where(p => p.Id == id && p.ArchivedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ArchivedAt, archivedAt), ct);

            if (updated != 0)
            {
                db.ProjectActivities.Add(ProjectActivity.Automatic(id, actorId, "پروژه آرشیو شد."));
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Results.NoContent();
            }

            return await db.Projects.AnyAsync(p => p.Id == id, ct)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }

    private static bool IsPanelRequest(HttpContext context) =>
        context.Request.Headers["X-Panel-Request"] == "1";

    private static bool IsValid(Project project) =>
        project.Name.Length is > 0 and <= 200 &&
        project.Status is "planned" or "active" or "paused" or "completed" &&
        project.Description?.Length is not > 4000 &&
        project.NextStep?.Length is not > 1000 &&
        IsWebUrl(project.RepositoryUrl) && IsWebUrl(project.EnvironmentUrl);

    private static bool IsWebUrl(string? value) =>
        value is null || (value.Length <= 2048 &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host));

    private sealed record ProjectInput(
        string? Name, string? Description, string? Status,
        string? RepositoryUrl, string? EnvironmentUrl, string? NextStep)
    {
        public Project ToProject(Guid id) => new()
        {
            Id = id,
            Name = Name?.Trim() ?? "",
            Description = Normalize(Description),
            Status = Status ?? "active",
            RepositoryUrl = Normalize(RepositoryUrl),
            EnvironmentUrl = Normalize(EnvironmentUrl),
            NextStep = Normalize(NextStep)
        };

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
