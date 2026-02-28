using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.PersonalInfoVault.Core.Commands.StorePersonalInfo;

public sealed record StorePersonalInfo(string PersonToken, string Name, string Email) : ICommand;
