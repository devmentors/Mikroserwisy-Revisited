using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Anonymization.Core.Exceptions;

public class AnonymizationConflictException(string personToken, Guid existingRequestId)
    : TicketFlowException($"Anonymization already in progress for: {personToken}, existing request: {existingRequestId}");
