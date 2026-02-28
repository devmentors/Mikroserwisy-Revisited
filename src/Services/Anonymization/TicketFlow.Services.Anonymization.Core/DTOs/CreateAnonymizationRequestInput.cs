namespace TicketFlow.Services.Anonymization.Core.DTOs;

public record CreateAnonymizationRequestInput(
    string? PersonToken,
    string? Email,
    string? RequestedByEmail);
