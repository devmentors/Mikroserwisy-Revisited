using TicketFlow.Shared.AnomalyGeneration.HttpApi;

namespace TicketFlow.Shared.AnomalyGeneration.Anomalies;

internal record ConsumerErrorBeforeHandlerAnomaly(string MessageType, string? StatusFilter = null) : IAnomaly
{
    public Task Run(string messageType, object? message)
    {
        if (messageType.Equals(MessageType) && message.StatusMatches(StatusFilter))
        {
            throw new AnomalyException();   
        }
        
        return Task.CompletedTask;
    }

    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(
            ((IAnomaly)this).AnomalyName, 
            MessageType, 
            new()
            {
                { nameof(StatusFilter), StatusFilter }
            });
    }
}

internal record ConsumerErrorAfterHandlerAnomaly(string MessageType, string? StatusFilter = null) : IAnomaly
{
    public Task Run(string messageType, object? message)
    {
        if (messageType.Equals(MessageType) && message.StatusMatches(StatusFilter))
        {
            throw new AnomalyException();   
        }
        
        return Task.CompletedTask;
    }

    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(
            ((IAnomaly)this).AnomalyName, 
            MessageType, 
            new()
            {
                { nameof(StatusFilter), StatusFilter }
            });
    }
}

internal record ConsumerDelayBeforeHandlerAnomaly(string MessageType, int DelayInMs, string? StatusFilter = null) : IAnomaly
{
    public async Task Run(string messageType, object? message)
    {
        if (messageType.Equals(MessageType) && message.StatusMatches(StatusFilter))
        {
            await Task.Delay(DelayInMs);
        }
    }

    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(
            ((IAnomaly)this).AnomalyName, 
            MessageType, 
            new()
            {
                { nameof(DelayInMs), DelayInMs.ToString() },
                { nameof(StatusFilter), StatusFilter }
            });
    }
}

internal record ConsumerDelayAfterHandlerAnomaly(string MessageType, int DelayInMs, string? StatusFilter = null) : IAnomaly
{
    public async Task Run(string messageType, object? message)
    {
        if (messageType.Equals(MessageType) && message.StatusMatches(StatusFilter))
        {
            await Task.Delay(DelayInMs);
        }
    }

    public AnomalyDescription Describe()
    {
        return new AnomalyDescription(
            ((IAnomaly)this).AnomalyName, 
            MessageType, 
            new()
            {
                { nameof(DelayInMs), DelayInMs.ToString() },
                { nameof(StatusFilter), StatusFilter }
            });
    }
}

internal static class MessageStatusChecker
{
    public static bool StatusMatches(this object? obj, string? expectedValue)
    {
        if (obj is null || string.IsNullOrWhiteSpace(expectedValue))
        {
            return true; // No filtering to be done
        }
        
        var statusProp = obj.GetType().GetProperty("Status");
        if (statusProp is null)
        {
            return true; // No filtering to be done either
        }
        
        var statusValue = statusProp.GetValue(obj);
        
        return (statusValue?.ToString() ?? "").Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
    }
} 