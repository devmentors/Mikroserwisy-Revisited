using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Anonymization.Core.Exceptions;

public class PersonTokenOrEmailRequiredException()
    : TicketFlowException("Either PersonToken or Email must be provided");
