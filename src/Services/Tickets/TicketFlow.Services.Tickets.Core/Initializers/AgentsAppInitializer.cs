using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Initializers;

internal sealed class AgentsAppInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TicketsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AgentsAppInitializer>>();
        
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Agents.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Agents are already initialized. Skipping further initialization.");
            return;
        }

        var agents = new List<Agent>
        {
            new(
                new Guid("00000000-0000-0000-0000-000000000001"),
                new Guid("00000000-0000-0000-0000-000000000001"),
                "Bogusław Złotówa",
                AgentPosition.Supervisor,
                "https://api.dicebear.com/9.x/notionists/svg?seed=Boguslaw&radius=50"),
            new(
                new Guid("00000000-0000-0000-0000-000000000002"), 
                new Guid("00000000-0000-0000-0000-000000000002"), 
                "Ziemowit Pędziwiatr",
                AgentPosition.Agent,
                "https://api.dicebear.com/9.x/notionists/svg?seed=Ziemowit&radius=50"),
            new (
                new Guid("00000000-0000-0000-0000-000000000003"), 
                new Guid("00000000-0000-0000-0000-000000000003"), 
                "Kunegunda Śmieszek",
                AgentPosition.Agent,
                "https://api.dicebear.com/9.x/notionists/svg?seed=Kunegunda&radius=50"),
        };
        
        await dbContext.AddRangeAsync(agents, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Agents initialized successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}