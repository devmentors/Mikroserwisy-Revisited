using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Anonymization.Core.Exceptions;

public class PersonNotFoundException(string identifier)
    : TicketFlowException($"Person not found: {identifier}");
