namespace TicketFlow.Shared.Observability;

public sealed class ObservabilityOptions
{
    public bool Enabled {get; set; }
    public string Endpoint {get; set; }
}