namespace TicketFlow.Services.Anonymization.Core.Http;

public record PersonalInfoDto(string PersonToken, string Name, string Email, bool IsAnonymized);
