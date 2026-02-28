namespace TicketFlow.Shared.AnomalyGeneration.HttpApi;

public interface IAnomaliesStorage
{
    void EnableAnomaly(EnableAnomalyRequest request);

    List<AnomalyDescription> GetAnomalies();

    void DisableAnomaly(AnomalyType typeToDisable, string messageType);
    
    void ResetAllAnomalies();
}