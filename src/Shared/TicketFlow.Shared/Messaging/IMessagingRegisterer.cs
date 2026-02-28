using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Messaging;

public interface IMessagingRegisterer
{
    internal IServiceCollection Services { get; }
    internal IConfiguration Configuration { get; }
}