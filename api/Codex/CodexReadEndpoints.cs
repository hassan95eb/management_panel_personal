using ManagementPanel.Api.Areas;
using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Codex;

public static class CodexReadEndpoints
{
    public static void MapCodexReadEndpoints(this WebApplication app)
    {
        app.MapGet("/api/codex/projects/{projectId:guid?}", ReadProjectsAsync)
            .AllowAnonymous(); // This handler authenticates Codex keys independently of owner cookies.
    }

    private static async Task<IResult> ReadProjectsAsync(
        Guid? projectId, HttpContext context, PanelDbContext db, CancellationToken ct)
    {
        var hash = CodexBearerKey.ReadHash(context.Request);
        if (hash is null) return Results.Unauthorized();
        var result = await db.CodexKeys.AsNoTracking()
            .AsSingleQuery()
            .Where(key => key.SecretHash == hash && key.RevokedAt == null)
            .Select(key => new
            {
                Projects = db.Projects
                    .Where(project => project.AreaKey == Area.ProjectsKey &&
                        project.ArchivedAt == null &&
                        (projectId == null || project.Id == projectId) &&
                        key.Grants.Any(grant => grant.ProjectId == project.Id && grant.CanRead))
                    .OrderBy(project => project.Name).ThenBy(project => project.Id)
                    .Select(project => new
                    {
                        project.Id,
                        project.Name,
                        project.Description,
                        project.Status,
                        project.RepositoryUrl,
                        project.EnvironmentUrl,
                        project.NextStep,
                        OpenTasks = db.ProjectTasks
                            .Where(task => task.ProjectId == project.Id &&
                                (task.Status == "todo" || task.Status == "in-progress" || task.Status == "blocked"))
                            .OrderBy(task => task.DueDate == null).ThenBy(task => task.DueDate)
                            .ThenBy(task => task.Title).ThenBy(task => task.Id)
                            .Select(task => new
                            {
                                task.Id,
                                task.Title,
                                task.Status,
                                task.Priority,
                                task.DueDate,
                                task.BlockedReason
                            }).ToList()
                    }).ToList()
            }).SingleOrDefaultAsync(ct);

        if (result is null) return Results.Unauthorized();
        if (projectId is null) return Results.Ok(result.Projects);
        return result.Projects.Count == 0 ? Results.NotFound() : Results.Ok(result.Projects[0]);
    }
}
