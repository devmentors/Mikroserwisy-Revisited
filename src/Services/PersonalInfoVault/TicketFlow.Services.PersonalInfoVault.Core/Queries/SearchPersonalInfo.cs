using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

public sealed record SearchPersonalInfo(string Query, int Limit = 50) : IQuery<SearchPersonalInfoDto>;

public record SearchPersonalInfoDto(List<PersonalInfoDto> Results);
