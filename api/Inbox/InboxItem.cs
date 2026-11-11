namespace ManagementPanel.Api.Inbox;

public sealed class InboxItem
{
    public Guid Id { get; init; }
    public required string Text { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
}
