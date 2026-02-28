namespace TicketFlow.Shared.AnomalyGeneration.HttpApi;

public record EnableAnomalyRequest(AnomalyType AnomalyType, string MessageType, Dictionary<string, string> AdditionalParams);