namespace TicketFlow.Services.Communication.Core.ExternalServices.Email;

public enum EmailServiceErrorCode
{
    Success = 0,
    ConfigurationError = 1,
    ValidationError = 2,
    QuotaExceeded = 3,
    TemporarilyUnavailable = 4,
    UnknownError = 999
}
