using Microsoft.Extensions.Logging;
using TicketFlow.Services.Communication.Core.Data;
using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Services.Communication.Core.Translations;

namespace TicketFlow.Services.Communication.Core.Messages;

internal sealed class MessageService(
    CommunicationDbContext dbContext,
    ILocalTranslationsService translationsService,
    ILogger<MessageService> logger) : IMessageService
{
    private const string DefaultLanguage = "pl";

    public async Task SaveMessageAsync(Message message, string? recipientLanguageCode = null, CancellationToken ct = default)
    {
        var targetLanguage = recipientLanguageCode ?? DefaultLanguage;

        if (targetLanguage != DefaultLanguage && !string.IsNullOrWhiteSpace(message.Content))
        {
            logger.LogInformation(
                "Translating message content to {Language} using LocalTranslationsService",
                targetLanguage);

            message.Content = await translationsService.TranslateAsync(
                message.Content,
                targetLanguage,
                ct);
        }

        await dbContext.Messages.AddAsync(message, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
