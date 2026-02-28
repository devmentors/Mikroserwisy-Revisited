namespace TicketFlow.BFF.Dashboard;

public record ManagerDashboardDto(
    List<TicketWithSlaDto> Tickets,
    DashboardSummaryDto Summary);

public record TicketWithSlaDto(
    string Id,
    string Name,
    string Email,
    string Title,
    string Category,
    string Status,
    string? SeverityLevel,
    DateTimeOffset CreatedAt,
    AgentInfoDto? Agent,
    SlaStatusDto? SlaStatus);

public record AgentInfoDto(
    string Id,
    string FullName,
    string Position,
    string AvatarUrl);

public record SlaStatusDto(
    DateTimeOffset DeadlineDateUtc,
    bool? DeadlineMet,
    bool ServiceCompleted,
    TimeRemainingDto? TimeRemaining,
    string SlaState);

public record TimeRemainingDto(
    int Days,
    int Hours,
    int Minutes,
    int TotalMinutes,
    bool IsOverdue);

public record DashboardSummaryDto(
    int TotalTickets,
    int OpenTickets,
    int BreachedSla,
    int AtRiskSla,
    int OnTrackSla,
    int CompletedOnTime,
    int CompletedLate);
