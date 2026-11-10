using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", async (DateOnly today, PanelDbContext db, CancellationToken ct) =>
        {
            const int limit = 10;
            var projects = db.Projects.AsNoTracking();
            var tasks = from task in db.ProjectTasks.AsNoTracking()
                        join project in projects on task.ProjectId equals project.Id
                        where project.ArchivedAt == null && task.Status != "done"
                        orderby task.DueDate == null, task.DueDate, task.Title, task.Id
                        select new
                        {
                            task.Id, task.ProjectId, ProjectName = project.Name,
                            task.Title, task.DueDate, task.BlockedReason, task.Status
                        };

            var dueToday = await tasks.Where(task => task.DueDate == today).Take(limit).ToListAsync(ct);
            var overdue = await tasks.Where(task => task.DueDate < today).Take(limit).ToListAsync(ct);
            var upcoming = await tasks.Where(task => task.DueDate > today).Take(limit).ToListAsync(ct);
            var blocked = await tasks.Where(task => task.Status == "blocked").Take(limit).ToListAsync(ct);

            var activeProjects = await projects.Where(project => project.ArchivedAt == null && project.Status == "active")
                .OrderBy(project => project.Name).ThenBy(project => project.Id)
                .Select(project => new { project.Id, project.Name, project.NextStep })
                .Take(limit).ToListAsync(ct);
            var recent = await (from activity in db.ProjectActivities.AsNoTracking()
                                join project in projects on activity.ProjectId equals project.Id
                                orderby activity.CreatedAt descending, activity.Id descending
                                select new
                                {
                                    activity.Id, activity.ProjectId, ProjectName = project.Name,
                                    activity.Report, activity.CreatedAt
                                }).Take(limit).ToListAsync(ct);

            return Results.Ok(new
            {
                DueToday = dueToday, Overdue = overdue, Upcoming = upcoming,
                Blocked = blocked, ActiveProjects = activeProjects, Recent = recent
            });
        }).RequireAuthorization(policy => policy.RequireRole("Owner"));
    }
}
