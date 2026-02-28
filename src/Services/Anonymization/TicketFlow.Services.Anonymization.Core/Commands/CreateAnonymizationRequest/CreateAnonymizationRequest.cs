using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Anonymization.Core.Commands.CreateAnonymizationRequest;

public sealed record CreateAnonymizationRequest(
    Guid RequestId,
    string? PersonToken,
    string? Email,
    string? RequestedByEmail) : ICommand;
