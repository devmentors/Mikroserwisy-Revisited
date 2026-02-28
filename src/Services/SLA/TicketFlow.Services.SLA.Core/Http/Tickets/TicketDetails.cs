using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Http.Tickets;

public class TicketDto(
    string Id,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    SeverityLevel? SeverityLevel,
    Guid? AssignedAgentUserId,
    string Type)
{
    public string Id { get; init; } = Id;
    public string Email { get; init; } = Email;
    public string Status { get; init; } = Status;
    public DateTimeOffset CreatedAt { get; init; } = CreatedAt;
    public SeverityLevel? SeverityLevel { get; init; } = SeverityLevel;
    public Guid? AssignedAgentUserId { get; init; } = AssignedAgentUserId;
    public string Type { get; init; } = Type;
}