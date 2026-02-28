using TicketFlow.Agents.KnowledgeBase.Data;

namespace TicketFlow.Agents.KnowledgeBase.Services;

public class FaqSearchService : IFaqSearchService
{
    private readonly ILogger<FaqSearchService> _logger;

    public FaqSearchService(ILogger<FaqSearchService> logger)
    {
        _logger = logger;
    }

    public List<FaqSearchResult> Search(string query, int maxResults = 3)
    {
        _logger.LogInformation("Searching FAQ for query: {Query}", query);

        var queryLower = query.ToLowerInvariant();
        var queryWords = queryLower
            .Split([' ', ',', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToArray();

        var results = new List<FaqSearchResult>();

        foreach (var faq in FaqDatabase.Entries)
        {
            var score = 0;
            var matchedKeywords = new List<string>();

            foreach (var keyword in faq.Keywords)
            {
                var keywordLower = keyword.ToLowerInvariant();
                if (queryLower.Contains(keywordLower))
                {
                    score += 10;
                    matchedKeywords.Add(keyword);
                }
                else if (SharesStem(queryLower, keywordLower))
                {
                    score += 8;
                    matchedKeywords.Add(keyword);
                }
            }

            foreach (var word in queryWords)
            {
                if (faq.Question.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score += 2;

                if (faq.Answer.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score += 1;
            }

            if (score > 0)
            {
                score += faq.Priority;
                results.Add(new FaqSearchResult
                {
                    FaqId = faq.Id,
                    Question = faq.Question,
                    Answer = faq.Answer,
                    Category = faq.Category,
                    Score = score,
                    MatchedKeywords = matchedKeywords
                });
            }
        }

        var topResults = results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();

        _logger.LogInformation("Found {Count} FAQ results (showing top {Max})",
            results.Count, topResults.Count);

        return topResults;
    }

    /// <summary>
    /// Checks if any word in the query shares a common stem (prefix) with the keyword.
    /// Handles Polish word forms like "hasłem" matching "hasło" (shared stem "hasł").
    /// </summary>
    private static bool SharesStem(string queryLower, string keywordLower)
    {
        const int minStemLength = 4;
        if (keywordLower.Length < minStemLength)
            return false;

        var queryWords = queryLower
            .Split([' ', ',', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in queryWords)
        {
            if (word.Length < minStemLength)
                continue;

            var commonLen = Math.Min(word.Length, keywordLower.Length);
            var stemLen = 0;
            for (var i = 0; i < commonLen; i++)
            {
                if (word[i] == keywordLower[i])
                    stemLen++;
                else
                    break;
            }

            if (stemLen >= minStemLength)
                return true;
        }

        return false;
    }
}
