using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox.Data;

namespace TicketFlow.Shared.Messaging.Outbox;

public static class Extensions
{
    private const string OutboxSection = "outbox";

    public static IMessagingRegisterer UseOutbox(this IMessagingRegisterer registerer, string sectionName = OutboxSection)
    {
        var enabled = registerer.Configuration.GetValue<bool>($"{sectionName}:enabled");
        
        if (enabled is false)
        {
            registerer.Services.AddPostgres<OutboxDbContext>(registerer.Configuration);
            return registerer;
        }
        
        var section = registerer.Configuration.GetSection(sectionName);
        registerer.Services.Configure<OutboxOptions>(section);
        registerer.Services.AddPostgres<OutboxDbContext>(registerer.Configuration);
        registerer.Services.AddScoped<IMessageOutbox, PostgresMessageOutbox>();
        registerer.Services.AddTransient<IMessagePublisher, OutboxMessagePublisher>();
        registerer.Services.AddHostedService<OutboxBackgroundService>();
        registerer.Services.AddSingleton<OutboxLocalCache>();
        registerer.Services.AddSingleton<OutboxPublishChannel>();
        registerer.Services.AddScoped<IMessageExecutionStep, OutboxBeforeExecutionStep>();
        registerer.Services.AddScoped<IMessageExecutionStep, OutboxAfterExecutionStep>();
        return registerer;
    }
}