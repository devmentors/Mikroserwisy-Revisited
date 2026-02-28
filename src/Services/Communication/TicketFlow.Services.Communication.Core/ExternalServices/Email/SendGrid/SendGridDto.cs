using System.Text.Json.Serialization;

namespace TicketFlow.Services.Communication.Core.ExternalServices.Email.SendGrid;

public record SendGridRequest
{
    [JsonPropertyName("personalizations")]
    public List<Personalization> Personalizations { get; init; } = [];

    [JsonPropertyName("from")]
    public EmailAddress From { get; init; } = default!;

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = default!;

    [JsonPropertyName("content")]
    public List<Content> Content { get; init; } = [];

    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; init; }
}

public record Personalization
{
    [JsonPropertyName("to")]
    public List<EmailAddress> To { get; init; } = [];

    [JsonPropertyName("cc")]
    public List<EmailAddress>? Cc { get; init; }

    [JsonPropertyName("bcc")]
    public List<EmailAddress>? Bcc { get; init; }
}

public record EmailAddress
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = default!;

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record Content
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text/plain";

    [JsonPropertyName("value")]
    public string Value { get; init; } = default!;
}

public record SendGridResponse
{
    [JsonPropertyName("message_id")]
    public string? MessageId { get; init; }
}
