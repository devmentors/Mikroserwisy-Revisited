using Microsoft.Extensions.Logging;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.PersonalInfoVault.Core.Commands.StorePersonalInfo;

internal sealed class StorePersonalInfoHandler(
    IPersonalInfoRepository repository,
    ILogger<StorePersonalInfoHandler> logger) : ICommandHandler<StorePersonalInfo>
{
    public async Task HandleAsync(
        StorePersonalInfo command,
        CancellationToken cancellationToken = default)
    {
        var personalInfo = new PersonalInfo(command.PersonToken, command.Name, command.Email);
        await repository.AddAsync(personalInfo, cancellationToken);

        logger.LogInformation("Stored PII for token {Token}", command.PersonToken);
    }
}
