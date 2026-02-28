namespace TicketFlow.Shared.Messaging.Resiliency;

internal class ReliablePublishing(ResiliencyOptions options)
{
    public bool UsePublisherConfirms => options.Producer.PublisherConfirmsEnabled;

    public bool ShouldPublishAsMandatory<TMessage>()
    {
        if (options.Producer.PublishMandatoryEnabled is false)
        {
            return false;
        }
        
        if (typeof(INonMandatoryMessage).IsAssignableFrom(typeof(TMessage)))
        {
            return false;
        }

        return true;
    }
}