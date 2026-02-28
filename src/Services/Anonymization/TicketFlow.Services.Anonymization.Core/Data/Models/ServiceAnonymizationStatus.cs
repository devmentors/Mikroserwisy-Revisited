namespace TicketFlow.Services.Anonymization.Core.Data.Models;

public class ServiceAnonymizationStatus
{
    public Guid Id { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public ServiceStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public Guid AnonymizationRequestId { get; private set; }
    public AnonymizationRequest? AnonymizationRequest { get; private set; }

    private ServiceAnonymizationStatus() { }

    public ServiceAnonymizationStatus(string serviceName)
    {
        Id = Guid.NewGuid();
        ServiceName = serviceName;
        Status = ServiceStatus.InProgress;
    }

    public void MarkCompleted(bool success, string? errorMessage = null)
    {
        Status = success ? ServiceStatus.Completed : ServiceStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public bool IsFinished => Status != ServiceStatus.InProgress;
    public bool IsSuccess => Status == ServiceStatus.Completed;
}
