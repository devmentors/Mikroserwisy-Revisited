namespace TicketFlow.Agents.Shared.A2A;

public record AgentCard
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string[] Capabilities { get; init; }
    public required string Endpoint { get; init; }
    public AgentCardSchema? InputSchema { get; init; }
    public AgentCardSchema? OutputSchema { get; init; }
}

public record AgentCardSchema
{
    public required string Type { get; init; }
    public Dictionary<string, AgentCardProperty>? Properties { get; init; }
    public string[]? Required { get; init; }
}

public record AgentCardProperty
{
    public required string Type { get; init; }
    public string? Description { get; init; }
    public string[]? Enum { get; init; }
}
