namespace TicketFlow.Services.Anonymization.Core.DTOs;

public record ServiceStatusDto(
    string ServiceName,
    bool Completed,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
