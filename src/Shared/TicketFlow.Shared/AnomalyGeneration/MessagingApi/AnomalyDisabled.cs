namespace TicketFlow.Shared.AnomalyGeneration.MessagingApi;

public record AnomalyDisabled(AnomalyType AnomalyType, string MessageType)
{
    public AnomalyEventWrapper Wrapped()
    {
        return new AnomalyEventWrapper(default, this);
    }
}