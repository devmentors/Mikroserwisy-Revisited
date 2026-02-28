using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Queries.ListTickets;

public record TicketsListDto(List<TicketsListEntryDto> Data, int TotalCount);

public record TicketsListEntryDto(
    string Id,
    string Name,
    string Email,
    string Title,
    string Description,
    string? DescriptionTranslated,
    TicketCategory Category,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    SeverityLevel? SeverityLevel,
    Guid? AgentId,
    string LanguageCode,
    TicketType? Type,
    DateTimeOffset? Deadline,
    string? Resolution = null);