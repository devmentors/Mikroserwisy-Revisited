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

        var slasToSeed = GetSlasToSeed();

        foreach (var sla in slasToSeed)
        {
            var exists = await dbContext.SignedSLAs
                .AnyAsync(x => x.Domain == sla.Domain, cancellationToken);

            if (!exists)
            {
                await dbContext.SignedSLAs.AddAsync(sla, cancellationToken);
                logger.LogInformation("Seeding SLA for {Domain}", sla.Domain);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<SignedSLA> GetSlasToSeed()
    {
        return
        [
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
                }),
            new SignedSLA("EXAMPLE INC.", "example.com", SLATier.VIP,
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
                }),
            new SignedSLA("TicketFlow Inc.", "ticketflow.com", SLATier.Premium,
                new Dictionary<ServiceType, SLADeadlines>
                {
                    {
                        ServiceType.IncidentTicket,
                        new SLADeadlines(new Dictionary<SeverityLevel, TimeSpan>
                        {
                            { SeverityLevel.Low, TimeSpan.FromMinutes(60) },
                            { SeverityLevel.Medium, TimeSpan.FromMinutes(30) },
                            { SeverityLevel.High, TimeSpan.FromMinutes(10) },
                            { SeverityLevel.Critical, TimeSpan.FromMinutes(5) }
                        })
                    },
                    {
                        ServiceType.QuestionTicket,
                        new SLADeadlines(new Dictionary<SeverityLevel, TimeSpan>
                        {
                            { SeverityLevel.Low, TimeSpan.FromMinutes(60) },
                            { SeverityLevel.Medium, TimeSpan.FromMinutes(30) },
                            { SeverityLevel.High, TimeSpan.FromMinutes(10) },
                            { SeverityLevel.Critical, TimeSpan.FromMinutes(5) }
                        })
                    },
                })
        ];
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
