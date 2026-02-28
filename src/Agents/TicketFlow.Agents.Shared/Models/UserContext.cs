namespace TicketFlow.Agents.Shared.Models;

public record UserContext
{
    public string? UserId { get; init; }
    public string Role { get; init; } = "client";
    public string Email { get; init; } = "unknown@example.com";
    public string? AgentId { get; init; }
}
