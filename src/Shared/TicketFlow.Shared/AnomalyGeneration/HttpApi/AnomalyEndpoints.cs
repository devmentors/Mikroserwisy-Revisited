using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.AnomalyGeneration.HttpApi;

public static class AnomalyEndpoints
{
    public static void UseAnomalyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("anomalies", ([FromServices] IAnomaliesStorage anomaliesStorage) =>
        {
            var anomalies = anomaliesStorage.GetAnomalies();
            return anomalies;
        });

        endpoints.MapPost("anomalies", async (
            [FromServices] IAnomaliesStorage anomaliesStorage,
            [FromServices] IEnumerable<IMessagePublisher> publishers,
            [FromServices] IOptions<AppOptions> appOptions,
            [FromBody] EnableAnomalyRequest request) =>
        {
            anomaliesStorage.EnableAnomaly(request);
            
            // Skip outbox to reduce the noise
            var publisher = publishers.FirstOrDefault(x => !x.GetType().Name.Contains("Outbox"));
            await publisher.PublishAsync(
                message: AnomalyEnabled.FromRequest(request).Wrapped(), 
                destination: AnomalyTopologyBuilder.AnomaliesExchange,
                routingKey: appOptions.Value.AppName);
        });
        
        endpoints.MapDelete("anomalies/{anomalyType}/messages/{messageType}", async (
            [FromServices] IAnomaliesStorage anomaliesStorage, 
            [FromServices] IEnumerable<IMessagePublisher> publishers,
            [FromServices] IOptions<AppOptions> appOptions,
            [FromRoute] string anomalyType,
            [FromRoute] string messageType) =>
        {
            var anomalyParsed = Enum.Parse<AnomalyType>(anomalyType);
            anomaliesStorage.DisableAnomaly(anomalyParsed, messageType);
            
            // Skip outbox to reduce the noise
            var publisher = publishers.FirstOrDefault(x => !x.GetType().Name.Contains("Outbox"));
            await publisher.PublishAsync(
                message: new AnomalyDisabled(anomalyParsed, messageType).Wrapped(), 
                destination: AnomalyTopologyBuilder.AnomaliesExchange,
                routingKey: appOptions.Value.AppName);
        });
    }
}