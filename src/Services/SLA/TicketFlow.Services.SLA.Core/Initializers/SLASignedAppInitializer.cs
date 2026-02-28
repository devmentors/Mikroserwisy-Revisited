using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Data;
using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Initializers;

internal sealed class SLASignedAppInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SLADbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SLASignedAppInitializer>>();
        
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.SignedSLAs.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Signed SLAs are already initialized. Skipping further initialization.");
            return;
        }

        var slas = new List<SignedSLA>
        {
            new SignedSLA("DevMentors", "devmentors.io", SLATier.VIP, 
                new Dictionary<ServiceType, SLADeadlines>
                {
                    {
                        ServiceType.IncidentTicket,
                        new SLADeadlines(new Dictionary<SeverityLevel, TimeSpan>
                            {
                                { SeverityLevel.Low, TimeSpan.FromMinutes(30) },
                                { SeverityLevel.Medium, TimeSpan.FromMinutes(15) },
                                { SeverityLevel.High, TimeSpan.FromMinutes(3) },
                                { SeverityLevel.Critical, TimeSpan.FromMinutes(1) }
                            })
                    },
                    {
                        ServiceType.QuestionTicket,
                        new SLADeadlines(new Dictionary<SeverityLevel, TimeSpan>
                        {
                            { SeverityLevel.Low, TimeSpan.FromMinutes(30) },
                            { SeverityLevel.Medium, TimeSpan.FromMinutes(15) },
                            { SeverityLevel.High, TimeSpan.FromMinutes(3) },
                            { SeverityLevel.Critical, TimeSpan.FromMinutes(1) }
                        })
                    },
                })
        };
        
        await dbContext.SignedSLAs.AddRangeAsync(slas, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Signed SLAs initialized successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}