using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.AnomalyGeneration;

public enum AnomalyType
{
    ConsumerErrorBeforeHandler,
    ConsumerErrorAfterHandler,
    ConsumerDelayBeforeHandler,
    ConsumerDelayAfterHandler,
    ProducerErrorBeforeTransaction,
    ProducerErrorWithinTransaction,
    ProducerErrorAfterTransaction,
    OutboxErrorOnSave,
    OutboxErrorOnPublish
}

internal static class AnomalyTypeExtensions
{
    public static AnomalyType AsAnomalyType(this ExecutionType executionType)
    {
        switch (executionType)
        {
            case ExecutionType.AfterTransaction:
                return AnomalyType.ProducerErrorAfterTransaction;
            case ExecutionType.BeforeTransaction:
                return AnomalyType.ProducerErrorBeforeTransaction;
            case ExecutionType.WithinTransaction:
                return AnomalyType.ProducerErrorWithinTransaction;
            default:
                throw new NotImplementedException(executionType.ToString());
        }
    }
}