namespace TicketFlow.Services.Aggregation.Core.Models;

public class TicketProjection
{
    public Guid Id { get; set; }
    public Guid InquiryId { get; set; }
    public string PersonToken { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string LanguageCode { get; set; }
    public string Status { get; set; }
    public string? SeverityLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? AgentId { get; set; }
    public string AgentName { get; set; }
    public string AgentAvatarUrl { get; set; }

    public DateTimeOffset? SlaDeadlineUtc { get; set; }
    public bool? SlaBreached { get; set; }
    public bool SlaServiceCompleted { get; set; }

    public int Version { get; set; }

    public static TicketProjection Create(
        Guid id,
        Guid inquiryId,
        string personToken,
        string title,
        string description,
        string category,
        string languageCode,
        int version)
    {
        return new TicketProjection
        {
            Id = id,
            InquiryId = inquiryId,
            PersonToken = personToken,
            Title = title,
            Description = description,
            Category = category,
            LanguageCode = languageCode,
            Status = "BeforeQualification",
            CreatedAt = DateTimeOffset.UtcNow,
            Version = version
        };
    }
}
