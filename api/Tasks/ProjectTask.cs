namespace ManagementPanel.Api.Tasks;

public sealed class ProjectTask
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public required string Title { get; set; }
    public string Status { get; set; } = "todo";
    public string Priority { get; set; } = "medium";
    public DateOnly? DueDate { get; set; }
    public string? BlockedReason { get; set; }
}
