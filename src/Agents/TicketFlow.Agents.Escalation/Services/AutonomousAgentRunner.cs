using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Models;
using TicketFlow.Agents.Shared.Langfuse;

namespace TicketFlow.Agents.Escalation.Services;

public sealed class AutonomousAgentRunner : IAutonomousAgentRunner
{
    private static readonly ActivitySource ActivitySource = new("TicketFlow.Agents.Escalation.AutonomousRunner", "1.0.0");

    private readonly IChatClient _chatClient;
    private readonly IPlanningService _planningService;
    private readonly ILlmReflectionService _llmReflectionService;
    private readonly ILangfuseClient _langfuse;
    private readonly EscalationOptions _options;
    private readonly ILogger<AutonomousAgentRunner> _logger;

    public AutonomousAgentRunner(
        IChatClient chatClient,
        IPlanningService planningService,
        ILlmReflectionService llmReflectionService,
        ILangfuseClient langfuse,
        IOptions<EscalationOptions> options,
        ILogger<AutonomousAgentRunner> logger)
    {
        _chatClient = chatClient;
        _planningService = planningService;
        _llmReflectionService = llmReflectionService;
        _langfuse = langfuse;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        string goal,
        AITool[] tools,
        int maxSteps = 10,
        CancellationToken ct = default)
    {
        using var runActivity = ActivitySource.StartActivity("autonomous-agent-run");
        SetActivityTags();
        var langfuseTrace = CreateLangfuseTrace(goal, tools, maxSteps, AmbientTraceContext.Current);

        var steps = new List<AgentStep>();
        int replanCount = 0;

        // === PLANNING PHASE ===
        AgentPlan plan = await CreateAgentPlan(goal, tools, ct, langfuseTrace);

        // == CONVERSATION SETUP ==
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GetAutonomousSystemPrompt(tools)),
            new(ChatRole.System, _planningService.FormatPlanForContext(plan)),
            new(ChatRole.User, goal)
        };

        for (int stepNumber = 1; stepNumber <= maxSteps; stepNumber++)
        {
            ct.ThrowIfCancellationRequested();

            using var stepActivity = ActivitySource.StartActivity($"autonomous-step-{stepNumber}", ActivityKind.Internal);
            stepActivity?.SetTag("step.number", stepNumber);
            stepActivity?.SetTag("step.max_steps", maxSteps);
            stepActivity?.SetTag("step.plan_version", plan.Version);

            var currentPlanStep = plan.GetCurrentStep();
            var planStepNumber = currentPlanStep?.StepNumber;
            
            if (currentPlanStep != null)
            {
                plan = _planningService.UpdateStepStatus(plan, currentPlanStep.StepNumber, PlanStepStatus.InProgress);
            }

            try
            {
                // === THOUGHT PHASE ===
                var thought = await CreateThoughtStep(ct, langfuseTrace, stepNumber, currentPlanStep, planStepNumber, plan, steps, stepActivity);

                // Inject thought into conversation so the action LLM is guided by it
                messages.Add(new ChatMessage(ChatRole.Assistant, thought));

                // === ACTION PHASE ===
                var (response, actionStep) = await Act(tools, ct, langfuseTrace, stepNumber, messages, planStepNumber, steps, stepActivity);

                TraceToolCalls(langfuseTrace, stepNumber, response);

                plan = UpdatePlanStep(currentPlanStep, actionStep, plan);

                // === REFLECTION PHASE ON FAILURE ===
                (plan, replanCount) = await ReflectIfFailure(tools, ct, actionStep, langfuseTrace, stepNumber, steps, planStepNumber, messages, plan, replanCount);

                // === GOAL CHECK ===
                var goalCheck = await CheckGoal(response, plan, steps, stepNumber, langfuseTrace, ct);

                if (goalCheck.IsAchieved)
                {
                    return await GoalAchieved(ct, stepActivity, runActivity, stepNumber, replanCount, langfuseTrace, response, steps, plan);
                }

                if (ShouldTerminate(response, steps))
                {
                    return await DecidedToTerminate(ct, stepNumber, stepActivity, runActivity, langfuseTrace, response, steps, plan, replanCount);
                }
            }
            catch (Exception ex)
            {
                (plan, var agentRunResult) = await StepFailed(maxSteps, ct, ex, stepNumber, steps, planStepNumber, stepActivity, currentPlanStep, runActivity, langfuseTrace, replanCount, messages, plan);
                return agentRunResult;
            }
        }
        
        LogMaxStepsReached(maxSteps, runActivity, replanCount);

        var finalSummary = await GenerateFinalSummaryAsync(messages, steps, tools, ct);

        _langfuse.UpdateTrace(langfuseTrace, finalSummary);
        await _langfuse.FlushAsync(ct);

        return AgentRunResult.MaxStepsReached(steps, plan, replanCount) with { FinalMessage = finalSummary };

        void SetActivityTags()
        {
            runActivity?.SetTag("agent.goal", goal.Length > 200 ? goal[..200] + "..." : goal);
            runActivity?.SetTag("agent.max_steps", maxSteps);
            runActivity?.SetTag("agent.tools_count", tools.Length);
            runActivity?.SetTag("agent.planning_enabled", _options.UsePlanning);
        }
    }

    private void LogMaxStepsReached(int maxSteps, Activity? runActivity, int replanCount)
    {
        _logger.LogWarning("Max steps ({MaxSteps}) reached without achieving goal", maxSteps);
        runActivity?.SetTag("agent.outcome", "max_steps_reached");
        runActivity?.SetTag("agent.total_steps", maxSteps);
        runActivity?.SetTag("agent.replan_count", replanCount);
    }

    private async Task<(AgentPlan plan, AgentRunResult agentRunResult)> StepFailed(int maxSteps, CancellationToken ct, Exception ex, int stepNumber, List<AgentStep> steps,
        int? planStepNumber, Activity? stepActivity, PlanStep? currentPlanStep, Activity? runActivity,
        LangfuseTrace langfuseTrace, int replanCount, List<ChatMessage> messages, AgentPlan plan)
    {
        _logger.LogError(ex, "Error at step {StepNumber}", stepNumber);
        steps.Add(AgentStep.Failed(stepNumber, ex.Message, planStepNumber: planStepNumber));
        stepActivity?.SetTag("step.error", true);
        stepActivity?.SetTag("step.error_message", ex.Message);
        stepActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        
        if (currentPlanStep != null)
        {
            plan = _planningService.UpdateStepStatus(plan, currentPlanStep.StepNumber, PlanStepStatus.Failed, ex.Message);
        }

        if (stepNumber == maxSteps)
        {
            runActivity?.SetTag("agent.outcome", "error");
            runActivity?.SetTag("agent.error_message", ex.Message);
            runActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _langfuse.UpdateTrace(langfuseTrace, $"ERROR: {ex.Message}");
            await _langfuse.FlushAsync(ct);

            return (plan, AgentRunResult.Error(
                $"Wystąpił błąd podczas przetwarzania: {ex.Message}",
                steps, plan, replanCount));
        }

        // Error context for recovery
        messages.Add(new ChatMessage(ChatRole.System,
            $"[BŁĄD SYSTEMU] Krok {stepNumber} zakończył się błędem: {ex.Message}. " +
            "Spróbuj alternatywnego podejścia lub użyj notify_supervisor jeśli problem jest krytyczny."));

        return default;
    }

    private async Task<AgentRunResult> DecidedToTerminate(CancellationToken ct, int stepNumber, Activity? stepActivity,
        Activity? runActivity, LangfuseTrace langfuseTrace, ChatResponse response, List<AgentStep> steps, AgentPlan plan,
        int replanCount)
    {
        _logger.LogInformation("Terminal state reached at step {StepNumber}", stepNumber);
        stepActivity?.SetTag("step.outcome", "terminal_state");
        runActivity?.SetTag("agent.outcome", "terminal_success");
        runActivity?.SetTag("agent.total_steps", stepNumber);

        _langfuse.UpdateTrace(langfuseTrace, response.Text);
        await _langfuse.FlushAsync(ct);

        return AgentRunResult.Success(
            response.Text ?? "Wykonano wszystkie możliwe akcje.",
            steps, plan, replanCount);
    }

    private async Task<AgentRunResult> GoalAchieved(CancellationToken ct, Activity? stepActivity, Activity? runActivity, int stepNumber,
        int replanCount, LangfuseTrace langfuseTrace, ChatResponse response, List<AgentStep> steps, AgentPlan plan)
    {
        _logger.LogInformation("[GOAL ✅] Achieved at step {Step} (replans: {Replans})", stepNumber, replanCount);
        stepActivity?.SetTag("step.outcome", "goal_achieved");
        runActivity?.SetTag("agent.outcome", "success");
        runActivity?.SetTag("agent.total_steps", stepNumber);
        runActivity?.SetTag("agent.replan_count", replanCount);

        _langfuse.UpdateTrace(langfuseTrace, response.Text);
        await _langfuse.FlushAsync(ct);

        return AgentRunResult.Success(response.Text ?? "Zadanie wykonane.", steps, plan, replanCount);
    }

    private async Task<(AgentPlan plan, int replanCount)> ReflectIfFailure(AITool[] tools, CancellationToken ct, AgentStep actionStep,
        LangfuseTrace langfuseTrace, int stepNumber, List<AgentStep> steps, int? planStepNumber, List<ChatMessage> messages, AgentPlan plan,
        int replanCount)
    {
        if (!actionStep.Success || ContainsFailurePattern(actionStep.ToolResult))
        {
            var reflectionSpan = _langfuse.CreateGeneration(
                langfuseTrace,
                name: $"reflection-{stepNumber}",
                model: GetModelName(),
                input: $"Failed step: {actionStep.ToolName} - {actionStep.ErrorMessage ?? actionStep.ToolResult}",
                metadata: new Dictionary<string, object>
                {
                    ["phase"] = "reflection",
                    ["failedTool"] = actionStep.ToolName ?? "unknown"
                });

            var reflection = await _llmReflectionService.ReflectOnFailureAsync(actionStep, plan, steps, ct);

            _langfuse.EndGeneration(reflectionSpan, JsonSerializer.Serialize(reflection));

            steps.Add(AgentStep.CreateReflection(stepNumber, reflection.Analysis, planStepNumber));
            _logger.LogInformation("[REFLECTION-{Step}] {Analysis}", stepNumber, reflection.Analysis);

            (replanCount, plan) = await ReplanIfNeeded(tools, ct, reflection, langfuseTrace, steps, messages, replanCount, plan);

            if (reflection.ShouldEscalateToSupervisor)
            {
                messages.Add(new ChatMessage(ChatRole.System,
                    "[ANALIZA WSKAZUJE] Musisz użyć notify_supervisor - sytuacja wymaga interwencji człowieka."));
            }
        }

        return (plan, replanCount);
    }

    private async Task<(int replanCount, AgentPlan plan)> ReplanIfNeeded(AITool[] tools, CancellationToken ct, ReflectionResult reflection,
        LangfuseTrace langfuseTrace, List<AgentStep> steps, List<ChatMessage> messages, int replanCount, AgentPlan plan)
    {
        if (reflection.ShouldReplan && replanCount < _options.MaxReplans)
        {
            var replanSpan = _langfuse.CreateSpan(
                langfuseTrace,
                name: $"replan-{replanCount + 1}",
                input: reflection.Analysis,
                metadata: new Dictionary<string, object>
                {
                    ["replanNumber"] = replanCount + 1,
                    ["maxReplans"] = _options.MaxReplans
                });

            plan = await _planningService.ReplanAsync(plan, reflection.Analysis, steps, tools, ct);
            replanCount++;

            _langfuse.EndSpan(replanSpan, JsonSerializer.Serialize(plan));

            _logger.LogInformation("[REPLAN-{Count}] Created new plan v{Version} with {StepCount} steps",
                replanCount, plan.Version, plan.Steps.Count);

            messages.Add(new ChatMessage(ChatRole.System, _planningService.FormatPlanForContext(plan)));
        }

        return (replanCount, plan);
    }

    private AgentPlan UpdatePlanStep(PlanStep? currentPlanStep, AgentStep actionStep, AgentPlan plan)
    {
        if (currentPlanStep != null)
        {
            var stepStatus = actionStep.Success && !ContainsFailurePattern(actionStep.ToolResult)
                ? PlanStepStatus.Completed
                : PlanStepStatus.Failed;
            plan = _planningService.UpdateStepStatus(plan, currentPlanStep.StepNumber, stepStatus, actionStep.ToolResult);
        }

        return plan;
    }

    private async Task<(ChatResponse response, AgentStep actionStep)> Act(AITool[] tools, CancellationToken ct, LangfuseTrace langfuseTrace, int stepNumber, List<ChatMessage> messages,
        int? planStepNumber, List<AgentStep> steps, Activity? stepActivity)
    {
        var actionGeneration = _langfuse.CreateGeneration(
            langfuseTrace,
            name: $"action-{stepNumber}",
            model: GetModelName(),
            input: messages.LastOrDefault()?.Text,
            metadata: new Dictionary<string, object>
            {
                ["phase"] = "action",
                ["stepNumber"] = stepNumber,
                ["toolsAvailable"] = tools.Length
            });

        var llmResponse = await _chatClient.GetResponseAsync(
            messages,
            new ChatOptions { Tools = tools },
            ct);

        messages.Add(llmResponse.Messages.Last());

        var toolCalls = llmResponse.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();

        ChatResponse response;
        if (toolCalls.Count > 0)
        {
            // Enforce one tool per REACT step — execute only the first call
            var firstToolCall = toolCalls[0];

            if (toolCalls.Count > 1)
            {
                _logger.LogWarning("[ACTION-{Step}] LLM requested {Count} tools, executing only first: {Tool}",
                    stepNumber, toolCalls.Count, firstToolCall.Name);
            }

            var tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == firstToolCall.Name);
            object? toolResult = null;

            if (tool != null)
            {
                try
                {
                    var args = firstToolCall.Arguments != null
                        ? new AIFunctionArguments(firstToolCall.Arguments)
                        : null;
                    toolResult = await tool.InvokeAsync(args, ct);
                    _logger.LogInformation("[ACTION-{Step}] Tool {Tool} executed successfully", stepNumber, firstToolCall.Name);
                }
                catch (Exception ex)
                {
                    toolResult = $"Error: {ex.Message}";
                    _logger.LogWarning(ex, "[ACTION-{Step}] Tool {Tool} failed", stepNumber, firstToolCall.Name);
                }
            }
            else
            {
                toolResult = $"Error: Tool '{firstToolCall.Name}' not found";
                _logger.LogWarning("[ACTION-{Step}] Tool {Tool} not found", stepNumber, firstToolCall.Name);
            }

            _logger.LogInformation("[OBSERVATION-{Step}] {Tool} → {Result}",
                stepNumber, firstToolCall.Name, toolResult?.ToString() ?? "no result");

            var toolResultContent = new FunctionResultContent(firstToolCall.CallId, toolResult);
            var toolResultMessage = new ChatMessage(ChatRole.Tool, [toolResultContent]);
            messages.Add(toolResultMessage);

            response = new ChatResponse(llmResponse.Messages.Append(toolResultMessage).ToList())
            {
                Usage = llmResponse.Usage
            };
        }
        else
        {
            response = llmResponse;
        }

        var usage = response.Usage;
        _langfuse.EndGeneration(
            actionGeneration,
            output: response.Text,
            inputTokens: usage?.InputTokenCount,
            outputTokens: usage?.OutputTokenCount);

        var actionStep = RecordStep(stepNumber, response, planStepNumber);
        steps.Add(actionStep);

        stepActivity?.SetTag("step.tool_name", actionStep.ToolName ?? "none");
        stepActivity?.SetTag("step.success", actionStep.Success);
        return (response, actionStep);
    }

    private async Task<string> CreateThoughtStep(CancellationToken ct, LangfuseTrace langfuseTrace, int stepNumber,
        PlanStep? currentPlanStep, int? planStepNumber, AgentPlan plan, List<AgentStep> steps, Activity? stepActivity)
    {
        var thoughtSpan = _langfuse.CreateGeneration(
            langfuseTrace,
            name: $"thought-{stepNumber}",
            model: GetModelName(),
            input: $"Plan step: {currentPlanStep?.Description ?? "N/A"}",
            metadata: new Dictionary<string, object>
            {
                ["phase"] = "thought",
                ["planStepNumber"] = planStepNumber ?? 0
            });

        var thought = await _llmReflectionService.GenerateThoughtAsync(currentPlanStep, plan, steps, ct);

        _langfuse.EndGeneration(thoughtSpan, thought);

        _logger.LogInformation("[THOUGHT-{Step}] {Thought}", stepNumber, thought);
        stepActivity?.SetTag("step.thought", thought.Length > 100 ? thought[..100] + "..." : thought);
        return thought;
    }


    private async Task<GoalCheckResult> CheckGoal(ChatResponse response, AgentPlan plan,
        IReadOnlyList<AgentStep> steps, int stepNumber, LangfuseTrace langfuseTrace, CancellationToken ct)
    {
        var goalCheckGeneration = _langfuse.CreateGeneration(
            langfuseTrace,
            name: $"goal-check-{stepNumber}",
            model: GetModelName(),
            input: response.Text,
            metadata: new Dictionary<string, object>
            {
                ["phase"] = "goal_check",
                ["stepNumber"] = stepNumber
            });

        var goalCheck = await _llmReflectionService.IsGoalAchievedAsync(response.Text, plan, steps, ct);

        _langfuse.EndGeneration(goalCheckGeneration, JsonSerializer.Serialize(goalCheck));

        return goalCheck;
    }

    private async Task<AgentPlan> CreateAgentPlan(string goal, AITool[] tools, CancellationToken ct, LangfuseTrace langfuseTrace)
    {
        AgentPlan plan;
        if (_options.UsePlanning)
        {
            var planSpan = _langfuse.CreateSpan(
                langfuseTrace,
                name: "planning-phase",
                input: goal,
                metadata: new Dictionary<string, object> { ["phase"] = "planning" });

            plan = await _planningService.CreatePlanAsync(goal, tools, ct);

            _logger.LogInformation("[PLAN] Created plan v{Version}: {Steps}",
                plan.Version,
                string.Join(" → ", plan.Steps.Select(s => s.ExpectedTool ?? s.Description)));

            _langfuse.EndSpan(planSpan, JsonSerializer.Serialize(plan));
        }
        else
        {
            plan = AgentPlan.CreateDefaultPlan(goal);
            _logger.LogInformation("[PLANNING] Using default plan (planning disabled)");
        }

        return plan;
    }

    private LangfuseTrace CreateLangfuseTrace(string goal, AITool[] tools, int maxSteps, LangfuseTraceContext? traceContext)
    {
        return _langfuse.CreateTrace(
            name: "escalation-agent-autonomous",
            input: goal,
            metadata: new Dictionary<string, object>
            {
                ["maxSteps"] = maxSteps,
                ["toolCount"] = tools.Length,
                ["framework"] = "Microsoft.Extensions.AI",
                ["provider"] = _options.Provider,
                ["planningEnabled"] = _options.UsePlanning,
                ["llmReflectionEnabled"] = _options.UseLlmReflection,
                ["parentTraceId"] = traceContext?.ParentTraceId ?? ""
            },
            userId: traceContext?.UserId,
            sessionId: traceContext?.SessionId);
    }

    private string GetModelName()
    {
        if (_options.Provider.Equals("openrouter", StringComparison.OrdinalIgnoreCase))
            return _options.OpenRouterModel;

        return _options.OllamaModel;
    }

    private string GetAutonomousSystemPrompt(AITool[] tools)
    {
        var toolDescriptions = new StringBuilder();
        foreach (var tool in tools)
        {
            if (tool is AIFunction func)
            {
                toolDescriptions.AppendLine($"- {func.Name}: {func.Description}");
            }
        }

        return $"""
            Jesteś AUTONOMICZNYM specjalistą ds. eskalacji i sam decydujesz o planie dzialania.

            WAŻNE: Odpowiadaj TYLKO po polsku. NIGDY nie używaj angielskiego ani innych języków!

            Działasz według PLANU WYKONANIA, który został dla Ciebie stworzony.
            Wykonuj plan KROK PO KROKU. BEZ dodatkowego inputu użytkownika podczas wykonywania.

            PATTERN REACT (jeden krok na raz):
            1. MYŚL (THOUGHT) - Co planuję zrobić i dlaczego
            2. AKCJA (ACTION) - Wywołaj DOKŁADNIE JEDNO narzędzie
            3. OBSERWACJA (OBSERVATION) - Przeanalizuj wynik
            4. REFLEKSJA - Jeśli błąd, przemyśl co poszło nie tak

            ⚠️ KRYTYCZNE: Wywołuj TYLKO JEDNO narzędzie na krok!
            NIE łącz wielu narzędzi w jednej odpowiedzi.
            Po każdym wywołaniu narzędzia CZEKAJ na wynik przed kolejnym krokiem.

            DOSTĘPNE NARZĘDZIA ({tools.Length}):
            {toolDescriptions}

            REGUŁY:
            1. WYKONUJ plan krok po kroku - JEDNO narzędzie na raz
            2. Jeśli krok się nie powiedzie, spróbuj alternatywy z planu
            3. Po osiągnięciu celu, napisz potwierdzenie zaczynające się od "✅"
            4. Dla ticketów CRITICAL - szybka eskalacja do supervisora
            5. NIE pytaj użytkownika o potwierdzenie - działaj autonomicznie!

            Masz do {_options.MaxSteps} kroków. Wykorzystaj je mądrze.

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private AgentStep RecordStep(int stepNumber, ChatResponse response, int? planStepNumber)
    {
        var toolCall = ExtractToolCallInfo(response);

        if (toolCall != null)
        {
            return AgentStep.Successful(
                stepNumber,
                toolName: toolCall.Value.Name,
                toolArguments: toolCall.Value.Arguments,
                toolResult: toolCall.Value.Result,
                llmResponse: response.Text,
                planStepNumber: planStepNumber);
        }

        return AgentStep.Successful(
            stepNumber,
            llmResponse: response.Text,
            planStepNumber: planStepNumber);
    }


    private bool ContainsFailurePattern(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lowerText = text.ToLowerInvariant();
        return lowerText.Contains("error") ||
               lowerText.Contains("failed") ||
               lowerText.Contains("not found");
    }

    private (string Name, string? Arguments, string? Result)? ExtractToolCallInfo(ChatResponse response)
    {
        var toolCalls = ExtractAllToolCalls(response);
        return toolCalls.FirstOrDefault();
    }

    private List<(string Name, string? Arguments, string? Result)> ExtractAllToolCalls(ChatResponse response)
    {
        var calls = new Dictionary<string, (string Name, string? Arguments)>();
        var results = new Dictionary<string, string?>();

        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    var argsJson = functionCall.Arguments != null
                        ? JsonSerializer.Serialize(functionCall.Arguments)
                        : null;
                    calls[functionCall.CallId ?? functionCall.Name] = (functionCall.Name, argsJson);
                }
                else if (content is FunctionResultContent functionResult)
                {
                    var resultStr = functionResult.Result?.ToString();
                    results[functionResult.CallId ?? "unknown"] = resultStr;
                }
            }
        }

        var toolCalls = new List<(string Name, string? Arguments, string? Result)>();
        foreach (var (callId, (name, args)) in calls)
        {
            results.TryGetValue(callId, out var result);
            toolCalls.Add((name, args, result));
        }

        return toolCalls;
    }

    private void TraceToolCalls(LangfuseTrace trace, int stepNumber, ChatResponse response)
    {
        var toolCalls = ExtractAllToolCalls(response);

        foreach (var (name, arguments, result) in toolCalls)
        {
            var span = _langfuse.CreateSpan(
                trace,
                name: $"tool:{name}",
                input: arguments,
                metadata: new Dictionary<string, object>
                {
                    ["stepNumber"] = stepNumber,
                    ["toolName"] = name
                });

            var isError = ContainsFailurePattern(result);

            _langfuse.EndSpan(
                span,
                output: result,
                level: isError ? "WARNING" : null);

            _logger.LogDebug("Traced tool call: {ToolName}, Success: {Success}", name, !isError);
        }
    }

    private bool ShouldTerminate(ChatResponse response, IReadOnlyList<AgentStep> steps)
    {
        var hasNoToolCalls = !response.Messages.Any(m =>
            m.Contents.Any(c => c is FunctionCallContent));

        var hasSuccessMarker = response.Text?.Contains("✅") == true;

        return hasNoToolCalls && hasSuccessMarker;
    }

    private async Task<string> GenerateFinalSummaryAsync(
        List<ChatMessage> messages,
        IReadOnlyList<AgentStep> steps,
        AITool[] tools,
        CancellationToken ct)
    {
        try
        {
            messages.Add(new ChatMessage(ChatRole.System,
                "Osiągnięto maksymalną liczbę kroków. Podsumuj co zostało wykonane i co wymaga dalszej uwagi. " +
                "Jeśli ticket nie został rozwiązany, zasugeruj eskalację do supervisora. " +
                "ODPOWIEDZ PO POLSKU."));

            var summaryResponse = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                ct);

            return summaryResponse.Text ?? "Osiągnięto limit kroków. Ticket wymaga ręcznej interwencji.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate final summary");
            return "Osiągnięto limit kroków. Ticket wymaga ręcznej interwencji.";
        }
    }
}
