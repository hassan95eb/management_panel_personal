namespace ManagementPanel.Api.Activities;

public sealed class ProjectActivity
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? TaskId { get; init; }
    public required string Report { get; init; }
    public string? Link { get; init; }
    public required string ActorId { get; init; }
    public required string Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    internal static ProjectActivity Automatic(Guid projectId, string actorId, string report, Guid? taskId = null) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        TaskId = taskId,
        ActorId = actorId,
        Report = report,
        Source = "automatic",
        CreatedAt = DateTimeOffset.UtcNow
    };
}
