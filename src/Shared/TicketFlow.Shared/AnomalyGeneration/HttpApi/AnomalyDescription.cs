namespace TicketFlow.Shared.AnomalyGeneration.HttpApi;

public record AnomalyDescription(string AnomalyType, string MessageType, Dictionary<string, string> Params);