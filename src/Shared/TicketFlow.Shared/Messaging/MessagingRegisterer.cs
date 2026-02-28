using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Messaging;

internal sealed record MessagingRegisterer(IServiceCollection Services, IConfiguration Configuration) : IMessagingRegisterer;