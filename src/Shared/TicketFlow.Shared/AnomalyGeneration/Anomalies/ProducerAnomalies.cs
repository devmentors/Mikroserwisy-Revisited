using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;

namespace TicketFlow.Shared.AnomalyGeneration.Anomalies;

internal record ProducerErrorBeforeTransactionAnomaly(string MessageType) : IAnomaly
{
    public Task Run(string messageType, object message)
    {
        if (messageType.Equals(MessageType))
        {
            throw new AnomalyException();   
        }
        
        return Task.CompletedTask;
    }
    
    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(((IAnomaly)this).AnomalyName, MessageType, new());
    }
}

internal record ProducerErrorWithinTransactionAnomaly(string MessageType) : IAnomaly
{
    public Task Run(string messageType, object message)
    {
        if (messageType.Equals(MessageType))
        {
            throw new AnomalyException();   
        }
        
        return Task.CompletedTask;
    }
    
    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(((IAnomaly)this).AnomalyName, MessageType, new());
    }
}

internal record ProducerErrorAfterTransactionAnomaly(string MessageType) : IAnomaly
{
    public Task Run(string messageType, object message)
    {
        if (messageType.Equals(MessageType))
        {
            throw new AnomalyException();   
        }
        
        return Task.CompletedTask;
    }
    
    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(((IAnomaly)this).AnomalyName, MessageType, new());
    }
}