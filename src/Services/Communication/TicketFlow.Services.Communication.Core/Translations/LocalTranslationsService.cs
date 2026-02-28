using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Caching;

namespace TicketFlow.Services.Communication.Core.Translations;

public interface ILocalTranslationsService
{
    Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default);
}

internal sealed class LocalTranslationsService(
    IChatClient? chatClient,
    ICacheService cacheService,
    ILogger<LocalTranslationsService> logger) : ILocalTranslationsService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    public async Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default)
    {
        if (chatClient is null)
        {
            logger.LogWarning("OpenAI not configured. Returning original text.");
            return text;
        }

        var cacheKey = ComputeCacheKey(text, targetLanguage);

        var cached = await cacheService.GetAsync<TranslationCacheEntry>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation(
                "Translation cache hit. Target: {Target}, TextHash: {Hash}",
                targetLanguage,
                cacheKey[..16]);
            return cached.TranslatedText;
        }

        logger.LogInformation("Translation cache miss. Calling OpenAI directly.");

        var prompt = $"Translate to {targetLanguage}: '{text}'";

        var response = await chatClient.CompleteAsync(prompt, cancellationToken: ct);
        var translatedText = response.Message.Text?.Trim() ?? text;

        var entry = new TranslationCacheEntry(translatedText);
        await cacheService.SetAsync(cacheKey, entry, CacheExpiry, ct);

        return translatedText;
    }

    private static string ComputeCacheKey(string text, string targetLanguage)
    {
        var input = $"{text}|{targetLanguage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"local_translation:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed record TranslationCacheEntry(string TranslatedText);
}

internal sealed class NoopLocalTranslationsService : ILocalTranslationsService
{
    public Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default)
        => Task.FromResult(text);
}
