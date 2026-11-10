using ManagementPanel.Api.Areas;

namespace ManagementPanel.Api.Projects;

public sealed class Project
{
    public Guid Id { get; init; }
    public string AreaKey { get; init; } = Area.ProjectsKey;
    public required string Name { get; set; }
}
