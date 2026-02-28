using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.PersonalInfoVault.Core.Exceptions;

public class PersonalInfoNotFoundException(string personToken)
    : TicketFlowException($"Personal info not found for token: {personToken}");
