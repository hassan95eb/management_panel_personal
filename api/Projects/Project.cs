using ManagementPanel.Api.Areas;

namespace ManagementPanel.Api.Projects;

public sealed class Project
{
    public Guid Id { get; init; }
    public string AreaKey { get; init; } = Area.ProjectsKey;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public string? RepositoryUrl { get; set; }
    public string? EnvironmentUrl { get; set; }
    public string? NextStep { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
