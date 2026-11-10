namespace ManagementPanel.Api.Areas;

public sealed class Area
{
    public const string ProjectsKey = "projects";

    public required string Key { get; init; }
    public required string Name { get; init; }
}
