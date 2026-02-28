using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Queries.GetTicketDetails;

public record TicketDetailsDto(
    string Id,
    Guid? UserId,
    string Title,
    string Description,
    string Email,
    string Status,
    string? Category,
    DateTimeOffset CreatedAt,
    SeverityLevel? SeverityLevel,
    Guid? AssignedAgentUserId,
    string? Type,
    string? Resolution = default);