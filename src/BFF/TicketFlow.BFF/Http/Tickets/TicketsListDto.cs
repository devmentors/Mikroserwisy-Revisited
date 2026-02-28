namespace TicketFlow.BFF.Http.Tickets;

public record TicketsListDto(List<TicketEntryDto> Data, int TotalCount);

public record TicketEntryDto(
    string Id,
    string Name,
    string Email,
    string Title,
    string Description,
    string? DescriptionTranslated,
    string Category,
    string Status,
    DateTimeOffset CreatedAt,
    string? SeverityLevel,
    Guid? AgentId,
    string LanguageCode,
    string? Type,
    DateTimeOffset? Deadline,
    string? Resolution = null);

public record AgentDto(
    string Id,
    string UserId,
    string FullName,
    string Position,
    string AvatarUrl);
