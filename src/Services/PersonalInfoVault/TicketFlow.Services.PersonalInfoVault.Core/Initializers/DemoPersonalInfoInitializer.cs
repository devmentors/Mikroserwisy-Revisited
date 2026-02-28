using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.PersonalInfoVault.Core.Data;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;
using TicketFlow.Shared.Authorization;

namespace TicketFlow.Services.PersonalInfoVault.Core.Initializers;

internal sealed class DemoPersonalInfoInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PersonalInfoVaultDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoPersonalInfoInitializer>>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // Check if demo clients already exist
        foreach (var demoClient in DemoUsers.Clients)
        {
            if (demoClient.PersonToken == null) continue;

            var exists = await dbContext.PersonalInfos
                .AnyAsync(p => p.PersonToken == demoClient.PersonToken, cancellationToken);

            if (exists)
            {
                logger.LogDebug("Demo client {Email} already exists, skipping", demoClient.Email);
                continue;
            }

            var personalInfo = new PersonalInfo(
                personToken: demoClient.PersonToken,
                name: GetNameFromEmail(demoClient.Email),
                email: demoClient.Email);

            dbContext.PersonalInfos.Add(personalInfo);
            logger.LogInformation("Created demo client: {Email} with PersonToken: {Token}",
                demoClient.Email, demoClient.PersonToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo personal info initialization completed");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private static string GetNameFromEmail(string email)
    {
        // Convert "jan.klient@example.com" to "Jan Klient"
        var localPart = email.Split('@')[0];
        var parts = localPart.Split('.');
        return string.Join(" ", parts.Select(p =>
            char.ToUpper(p[0]) + p[1..].ToLower()));
    }
}
