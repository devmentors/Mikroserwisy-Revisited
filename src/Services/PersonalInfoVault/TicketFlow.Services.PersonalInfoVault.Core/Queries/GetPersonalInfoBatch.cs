using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

public sealed record GetPersonalInfoBatch(List<string> Tokens) : IQuery<BatchResponse>;
