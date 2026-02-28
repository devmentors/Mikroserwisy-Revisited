using TicketFlow.Shared.AnomalyGeneration.HttpApi;

namespace TicketFlow.Shared.AnomalyGeneration.Anomalies;

internal interface IAnomaly
{
    Task Run(string messageType, object? message);
    
    string MessageType { get; }

    public string AnomalyName
    {
        get
        {
            return GetType().Name.Replace("Anomaly", string.Empty);            
        }
    }
    AnomalyDescription Describe();
}