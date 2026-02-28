using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Queries.GetTicketDetails;

public record TicketDetailsDto(
    string Id,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    SeverityLevel? SeverityLevel,
    Guid? AssignedAgentUserId,
    string? Type,
    string? Resolution = default);