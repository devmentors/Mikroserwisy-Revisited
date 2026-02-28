using TicketFlow.Shared.AnomalyGeneration.Anomalies;
using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox;

namespace TicketFlow.Shared.AnomalyGeneration;

internal class AnomalyGenerator : IConsumerAnomalyGenerator, IProducerAnomalyGenerator, IOutboxAnomalyGenerator, IAnomaliesStorage
{
    private readonly Dictionary<AnomalyType, List<IAnomaly>> _anomalies = new();

    public AnomalyGenerator()
    {
        foreach (var enumValue in Enum.GetValues(typeof(AnomalyType)))
        {
            _anomalies.Add((AnomalyType)enumValue, new List<IAnomaly>());
        }
    }
    
    public void EnableAnomaly(EnableAnomalyRequest request)
    {
        var anomaly = MapFromRequest(request);
        if (_anomalies[request.AnomalyType]
            .Any(x => x.MessageType == anomaly.MessageType && x.AnomalyName == anomaly.AnomalyName))
        {
            return;
        }
        _anomalies[request.AnomalyType].Add(anomaly);
    }

    public List<AnomalyDescription> GetAnomalies()
    {
        return _anomalies.Values.SelectMany(x => x).Select(x => x.Describe()).ToList();
    }

    public void DisableAnomaly(AnomalyType typeToDisable, string messageType)
    {
        var anomaliesEnabled = _anomalies.TryGetValue(typeToDisable, out var anomalies);
        if (!anomaliesEnabled || anomalies is null) return;

        var anomaliesLeft = anomalies.ToList();
        foreach (var anomaly in anomalies.Where(x => x.MessageType.Equals(messageType)))
        {
            anomaliesLeft.Remove(anomaly);
        }
        
        _anomalies[typeToDisable] = anomaliesLeft;
        
    }

    public void ResetAllAnomalies()
    {
        foreach (var type in _anomalies.Keys)
        {
            _anomalies[type].Clear();
        }
    }

    public async Task GenerateBeforeHandlerAsync<TMessage>(TMessage message)
    {
        if (message is null)
        {
            return;
        }
        
        var anomaliesToRun = _anomalies[AnomalyType.ConsumerDelayBeforeHandler]
            .Concat(_anomalies[AnomalyType.ConsumerErrorBeforeHandler]);

        foreach (var anomaly in anomaliesToRun)
        {
            await anomaly.Run(message.GetType().Name, message);
        }
    }

    public async Task GenerateAfterHandlerAsync<TMessage>(TMessage message)
    {
        if (message is null)
        {
            return;
        }
        
        var anomaliesToRun = _anomalies[AnomalyType.ConsumerDelayAfterHandler]
            .Concat(_anomalies[AnomalyType.ConsumerErrorAfterHandler]);

        foreach (var anomaly in anomaliesToRun)
        {
            await anomaly.Run(message.GetType().Name, message);
        }
    }

    public async Task GenerateOnProduceAsync(string messageType, ExecutionType when)
    {
        if (string.IsNullOrEmpty(messageType))
        {
            return;
        }

        foreach (var anomaly in _anomalies[when.AsAnomalyType()])
        {
            await anomaly.Run(messageType, default);
        }
    }

    public async Task GenerateOnSaveAsync(OutboxMessage message)
    {
        if (message is null)
        {
            return;
        }

        foreach (var anomaly in _anomalies[AnomalyType.OutboxErrorOnSave])
        {
            await anomaly.Run(message.Message.GetType().Name, message.Message);
        }
    }

    public async Task GenerateOnPublishAsync(OutboxMessage message)
    {
        if (message is null)
        {
            return;
        }

        foreach (var anomaly in _anomalies[AnomalyType.OutboxErrorOnPublish])
        {
            await anomaly.Run(message.Message.GetType().Name, message.Message);
        }
    }

    private static IAnomaly MapFromRequest(EnableAnomalyRequest request)
    {
        const string delayParamName = "DelayInMs";
        var delayParamExists = request.AdditionalParams.TryGetValue(delayParamName, out var delayParam);
        var delayInMs = 1;
        var delayParseSuccess = delayParamExists && int.TryParse(delayParam, out delayInMs);
        
        const string statusFilterParamName = "StatusFilter";
        request.AdditionalParams.TryGetValue(statusFilterParamName, out var statusFilter);
        
        switch (request.AnomalyType)
        {
            case AnomalyType.ConsumerDelayBeforeHandler:
                if (delayParseSuccess is false) { throw new ArgumentNullException(delayParamName); }
                return new ConsumerDelayBeforeHandlerAnomaly(request.MessageType, delayInMs, statusFilter);
            case AnomalyType.ConsumerDelayAfterHandler:
                if (delayParseSuccess is false) { throw new ArgumentNullException(delayParamName); }
                return new ConsumerDelayAfterHandlerAnomaly(request.MessageType, delayInMs, statusFilter);
            case AnomalyType.ConsumerErrorBeforeHandler:
                return new ConsumerErrorBeforeHandlerAnomaly(request.MessageType, statusFilter);
            case AnomalyType.ConsumerErrorAfterHandler:
                return new ConsumerErrorAfterHandlerAnomaly(request.MessageType, statusFilter);
            case AnomalyType.ProducerErrorBeforeTransaction:
                return new ProducerErrorBeforeTransactionAnomaly(request.MessageType);
            case AnomalyType.ProducerErrorWithinTransaction:
                return new ProducerErrorWithinTransactionAnomaly(request.MessageType);
            case AnomalyType.ProducerErrorAfterTransaction:
                return new ProducerErrorAfterTransactionAnomaly(request.MessageType);
            case AnomalyType.OutboxErrorOnSave:
                return new OutboxErrorOnSaveAnomaly(request.MessageType);
            case AnomalyType.OutboxErrorOnPublish:
                return new OutboxErrorOnPublishAnomaly(request.MessageType);
            default:
                throw new NotImplementedException(request.AnomalyType.ToString());
        }
    }
}