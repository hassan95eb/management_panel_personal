namespace ManagementPanel.Api.Codex;

public sealed class CodexKey
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string SecretHash { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public List<CodexProjectGrant> Grants { get; init; } = [];
}

public sealed class CodexProjectGrant
{
    public Guid KeyId { get; init; }
    public Guid ProjectId { get; init; }
    public bool CanRead { get; init; }
    public bool CanWriteActivity { get; init; }
}
