namespace TicketFlow.Services.Anonymization.Core.DTOs;

public record AnonymizationRequestDto(
    Guid Id,
    string PersonToken,
    string RequestedByEmail,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    List<ServiceStatusDto> ServiceStatuses);
