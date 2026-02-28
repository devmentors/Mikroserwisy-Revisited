namespace TicketFlow.Services.PersonalInfoVault.Core.Services;

public class TokenGenerator : ITokenGenerator
{
    public string Generate()
    {
        var uniquePart = Guid.NewGuid().ToString("N")[..12];
        return $"person_{uniquePart}";
    }
}
