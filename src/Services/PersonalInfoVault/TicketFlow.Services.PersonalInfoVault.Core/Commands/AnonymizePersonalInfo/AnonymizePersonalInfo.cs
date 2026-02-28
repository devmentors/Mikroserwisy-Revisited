using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.PersonalInfoVault.Core.Commands.AnonymizePersonalInfo;

public sealed record AnonymizePersonalInfo(string PersonToken) : ICommand;
