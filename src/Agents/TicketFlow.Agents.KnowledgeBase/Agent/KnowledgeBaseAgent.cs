using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using TicketFlow.Agents.KnowledgeBase.Services;
using TicketFlow.Agents.Shared.Langfuse;

namespace TicketFlow.Agents.KnowledgeBase.Agent;

public class KnowledgeBaseAgent
{
    private static readonly ActivitySource ActivitySource = new("TicketFlow.Agents.KnowledgeBase", "1.0.0");

    private readonly IChatClient _chatClient;
    private readonly IFaqSearchService _faqSearch;
    private readonly ITicketsMcpClient _ticketsMcp;
    private readonly ILangfuseClient _langfuse;
    private readonly ILogger<KnowledgeBaseAgent> _logger;

    public KnowledgeBaseAgent(
        IChatClient chatClient,
        IFaqSearchService faqSearch,
        ITicketsMcpClient ticketsMcp,
        ILangfuseClient langfuse,
        ILogger<KnowledgeBaseAgent> logger)
    {
        _chatClient = chatClient;
        _faqSearch = faqSearch;
        _ticketsMcp = ticketsMcp;
        _langfuse = langfuse;
        _logger = logger;

        Tools = CreateTools();
    }

    public AITool[] Tools { get; }

    public string SystemPrompt => BuildSystemPrompt();

    public async Task<KnowledgeBaseResponse> HandleQueryAsync(string query)
    {
        using var activity = ActivitySource.StartActivity("knowledgebase-query", ActivityKind.Internal);
        activity?.SetTag("kb.query", query.Length > 200 ? query[..200] + "..." : query);
        activity?.SetTag("kb.tools_count", Tools.Length);

        var ctx = AmbientTraceContext.Current;
        var langfuseTrace = _langfuse.CreateTrace(
            name: "knowledgebase-query",
            input: query,
            metadata: new Dictionary<string, object>
            {
                ["tools_count"] = Tools.Length,
                ["max_iterations"] = 5
            },
            userId: ctx?.UserId,
            sessionId: ctx?.SessionId);

        if (ctx != null) ctx.CurrentTraceId = langfuseTrace.Id;

        _logger.LogInformation("KB Agent processing query: {Query}", query);

        try
        {
            var functionInvokingClient = new FunctionInvokingChatClient(_chatClient)
            {
                MaximumIterationsPerRequest = 5
            };

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, query)
            };

            var response = await functionInvokingClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = Tools });

            var responseText = response.Text ?? "Nie udało się wygenerować odpowiedzi.";

            var result = new KnowledgeBaseResponse
            {
                Success = true,
                Source = "LLM+Tools",
                Message = responseText,
                StepCount = response.Messages.Count(m =>
                    m.Contents.Any(c => c is FunctionCallContent))
            };

            activity?.SetTag("kb.success", true);
            activity?.SetTag("kb.source", result.Source);
            activity?.SetTag("kb.step_count", result.StepCount);
            activity?.SetTag("kb.response_length", responseText.Length);

            _langfuse.UpdateTrace(langfuseTrace, new { success = true, source = result.Source, stepCount = result.StepCount });
            await _langfuse.FlushAsync();

            _logger.LogInformation("KB Agent completed in {Steps} steps, response length: {Length}", result.StepCount, responseText.Length);

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("kb.success", false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _langfuse.UpdateTrace(langfuseTrace, $"ERROR: {ex.Message}");
            await _langfuse.FlushAsync();

            throw;
        }
    }

    private AITool[] CreateTools()
    {
        var searchFaq = AIFunctionFactory.Create(
            (string query, int maxResults = 3) =>
            {
                _logger.LogInformation("[TOOL] search_faq called with query: {Query}", query);
                var results = _faqSearch.Search(query, maxResults);
                _logger.LogInformation("[TOOL] search_faq returned {Count} results", results.Count);
                return JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            },
            new AIFunctionFactoryOptions
            {
                Name = "search_faq",
                Description = "Przeszukaj bazę FAQ (frequently asked questions) z pytaniami i odpowiedziami. " +
                              "Zwraca listę wyników z pytaniem, odpowiedzią i wynikiem dopasowania (score). " +
                              "Użyj tego narzędzia najpierw - FAQ zawiera najczęściej zadawane pytania."
            });

        var searchResolvedTickets = AIFunctionFactory.Create(
            async (string query, string? category = null, int limit = 3) =>
            {
                _logger.LogInformation("[TOOL] search_resolved_tickets called with query: {Query}", query);
                var results = await _ticketsMcp.SearchResolvedTicketsAsync(query, category, limit);
                _logger.LogInformation("[TOOL] search_resolved_tickets returned {Count} results", results.Count);
                return JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            },
            new AIFunctionFactoryOptions
            {
                Name = "search_resolved_tickets",
                Description = "Przeszukaj historyczne rozwiązane tickety przez MCP Server. " +
                              "Zwraca tickety z tytułem, opisem, rozwiązaniem i datą. " +
                              "Użyj gdy FAQ nie daje wystarczającej odpowiedzi lub chcesz uzupełnić informacje."
            });

        return [searchFaq, searchResolvedTickets];
    }

    private static string BuildSystemPrompt() =>
        """
        Jesteś agentem bazy wiedzy TicketFlow. Twoim zadaniem jest znajdowanie odpowiedzi na pytania
        korzystając z dostępnych narzędzi.

        STRATEGIA WYSZUKIWANIA:
        1. Zacznij od search_faq - przeszukaj bazę najczęściej zadawanych pytań
        2. Jeśli wyniki FAQ są niewystarczające (niski score lub brak trafnych odpowiedzi),
           użyj search_resolved_tickets żeby sprawdzić historyczne rozwiązania
        3. Zsyntetyzuj odpowiedź na podstawie zebranych informacji

        JAK SZUKAĆ W FAQ:
        - Przekazuj KRÓTKIE, KLUCZOWE słowa w formie podstawowej (np. "hasło reset", "faktura", "eskalacja ticket")
        - NIE przekazuj pełnych zdań, identyfikatorów ticketów ani UUID-ów
        - Wyodrębnij kluczowy temat z zapytania i użyj 1-3 słów kluczowych
        - Jeśli pierwsze wyszukiwanie daje wyniki z niskim score, spróbuj z innymi synonimami
        - Przykład: zapytanie "problemu z hasłem do konta" → szukaj "hasło reset"

        ZASADY:
        - Odpowiadaj po polsku
        - Podawaj konkretne informacje ze znalezionych źródeł
        - Jeśli wyniki FAQ mają matchedKeywords i score > 10, to trafne dopasowanie — użyj ich
        - Jeśli nic nie znalazłeś w żadnym źródle, powiedz o tym wprost
        - Nie wymyślaj informacji których nie ma w wynikach wyszukiwania
        - Bądź zwięzły ale pomocny
        """;
}
