namespace TicketFlow.Agents.Shared.A2A;

public enum AgentTaskStatus
{
    Submitted,
    Working,
    Completed,
    Failed
}

/// <summary>
/// Response from an agent task execution.
/// </summary>
public record AgentTaskResponse
{
    public required string TaskId { get; init; }
    public required AgentTaskStatus Status { get; init; }
    public object? Output { get; init; }
    public string? Error { get; init; }
    public string? Reasoning { get; init; }
}
