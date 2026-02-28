namespace TicketFlow.Services.PersonalInfoVault.Core.DTOs;

public record PersonalInfoDto(string PersonToken, string Name, string Email, bool IsAnonymized);
