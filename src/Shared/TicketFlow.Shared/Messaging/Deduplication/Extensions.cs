using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Messaging.Deduplication.Data;
using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.Messaging.Deduplication;

public static class Extensions
{
    private const string DeduplicationSection = "deduplication";

    public static IMessagingRegisterer UseDeduplication(this IMessagingRegisterer registerer, string sectionName = DeduplicationSection)
    {
        var enabled = registerer.Configuration.GetValue<bool>($"{sectionName}:enabled");
        
        if (enabled is false)
        {
            registerer.Services.AddScoped<IDeduplicationStore, NoopDeduplicationStore>();
            // Required, so that initial migration succeeds
            registerer.Services.AddPostgres<DeduplicationDbContext>(registerer.Configuration);
            return registerer;
        }
        
        var section = registerer.Configuration.GetSection(sectionName);
        registerer.Services.Configure<DeduplicationOptions>(section);
        registerer.Services.AddPostgres<DeduplicationDbContext>(registerer.Configuration);
        registerer.Services.AddScoped<IDeduplicationStore, PostgresDeduplicationStore>();
        registerer.Services.AddScoped<IMessageExecutionStep, DeduplicationBeforeExecutionStep>();
        registerer.Services.AddScoped<IMessageExecutionStep, DeduplicationTransactionExecutionStep>();
        return registerer;
    }
}