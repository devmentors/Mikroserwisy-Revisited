using Microsoft.Extensions.Logging;
using TicketFlow.Services.PersonalInfoVault.Core.Exceptions;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.PersonalInfoVault.Core.Commands.AnonymizePersonalInfo;

internal sealed class AnonymizePersonalInfoHandler(
    IPersonalInfoRepository repository,
    ILogger<AnonymizePersonalInfoHandler> logger) : ICommandHandler<AnonymizePersonalInfo>
{
    public async Task HandleAsync(
        AnonymizePersonalInfo command,
        CancellationToken cancellationToken = default)
    {
        var personalInfo = await repository.GetByTokenAsync(command.PersonToken, cancellationToken);
        if (personalInfo is null)
        {
            throw new PersonalInfoNotFoundException(command.PersonToken);
        }

        if (personalInfo.IsAnonymized)
        {
            logger.LogInformation("PII for token {PersonToken} already anonymized", command.PersonToken);
            return;
        }

        personalInfo.Anonymize();
        await repository.UpdateAsync(personalInfo, cancellationToken);

        logger.LogInformation("Anonymized PII for token {PersonToken}", command.PersonToken);
    }
}
