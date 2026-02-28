using TicketFlow.Shared.AnomalyGeneration.HttpApi;

namespace TicketFlow.Shared.AnomalyGeneration.MessagingApi;

public record AnomalyEnabled(
    AnomalyType AnomalyType,
    string MessageType,
    Dictionary<string, string> AdditionalParams)
{
    public static AnomalyEnabled FromRequest(EnableAnomalyRequest request)
    {
        return new AnomalyEnabled(request.AnomalyType, request.MessageType, request.AdditionalParams);
    }

    public AnomalyEventWrapper Wrapped()
    {
        return new AnomalyEventWrapper(this, default);
    }
}
