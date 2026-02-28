namespace TicketFlow.Services.Aggregation.Core.Http;

public record PersonalInfoDto(string PersonToken, string Name, string Email, bool IsAnonymized);
