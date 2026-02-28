namespace TicketFlow.Shared.Messaging.Resiliency;

public class ResiliencyOptions
{
    public ResiliencyOptions(ConsumerResiliencyOptions consumer, ProducerResiliencyOptions producer)
    {
        Consumer = consumer;
        Producer = producer;
    }

    public ConsumerResiliencyOptions Consumer { get; private set; }
    public ProducerResiliencyOptions Producer { get; private set; }
}

public record ConsumerResiliencyOptions(
    bool BrokerRetriesEnabled,
    int BrokerRetriesLimit,
    bool ConsumerRetriesEnabled,
    int ConsumerRetriesLimit,
    bool UseDeadletter,
    bool PublishFaultOnFailure,
    int MaxMessagesFetchedPerConsumer);
    
public record ProducerResiliencyOptions(bool PublisherConfirmsEnabled, bool PublishMandatoryEnabled);