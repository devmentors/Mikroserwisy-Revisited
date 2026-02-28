namespace TicketFlow.Services.PersonalInfoVault.Core.Data.Models;

public class PersonalInfo
{
    public Guid Id { get; set; }
    public string PersonToken { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAnonymized { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AnonymizedAt { get; set; }

    private PersonalInfo() { }

    public PersonalInfo(string personToken, string name, string email)
    {
        Id = Guid.NewGuid();
        PersonToken = personToken;
        Name = name;
        Email = email;
        IsAnonymized = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Anonymize()
    {
        Name = "***";
        Email = "anonymized@example.com";
        IsAnonymized = true;
        AnonymizedAt = DateTimeOffset.UtcNow;
    }
}
