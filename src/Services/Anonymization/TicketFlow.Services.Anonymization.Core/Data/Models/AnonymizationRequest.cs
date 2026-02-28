namespace TicketFlow.Services.Anonymization.Core.Data.Models;

public class AnonymizationRequest
{
    public Guid Id { get; private set; }
    public string PersonToken { get; private set; } = string.Empty;
    public string RequestedByEmail { get; private set; } = string.Empty;
    public AnonymizationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<ServiceAnonymizationStatus> _serviceStatuses = new();
    public IReadOnlyCollection<ServiceAnonymizationStatus> ServiceStatuses => _serviceStatuses.AsReadOnly();

    private AnonymizationRequest() { }

    public AnonymizationRequest(Guid id, string personToken, string requestedByEmail)
    {
        Id = id;
        PersonToken = personToken;
        RequestedByEmail = requestedByEmail;
        Status = AnonymizationStatus.InProgress;
        CreatedAt = DateTimeOffset.UtcNow;

        // Expected services to respond
        _serviceStatuses.Add(new ServiceAnonymizationStatus("vault"));
        _serviceStatuses.Add(new ServiceAnonymizationStatus("tickets"));
        _serviceStatuses.Add(new ServiceAnonymizationStatus("aggregation"));
    }

    public void MarkServiceCompleted(string serviceName, bool success, string? errorMessage = null)
    {
        var serviceStatus = _serviceStatuses.FirstOrDefault(s => s.ServiceName == serviceName);
        if (serviceStatus is null) return;

        serviceStatus.MarkCompleted(success, errorMessage);

        // Check if all services finished
        if (_serviceStatuses.All(s => s.IsFinished))
        {
            Status = _serviceStatuses.All(s => s.IsSuccess)
                ? AnonymizationStatus.Completed
                : AnonymizationStatus.PartiallyCompleted;
            CompletedAt = DateTimeOffset.UtcNow;
        }
    }
}

public enum AnonymizationStatus
{
    InProgress,
    Completed,
    PartiallyCompleted,
    Failed
}
