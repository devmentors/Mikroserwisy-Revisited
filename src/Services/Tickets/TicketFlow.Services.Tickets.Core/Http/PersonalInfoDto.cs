namespace TicketFlow.Services.Tickets.Core.Http;

public record PersonalInfoDto(string PersonToken, string Name, string Email, bool IsAnonymized);
