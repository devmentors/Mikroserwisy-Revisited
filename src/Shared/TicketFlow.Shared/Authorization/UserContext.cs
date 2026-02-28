namespace TicketFlow.Shared.Authorization;

public record UserContext(
    string Email,
    UserRole Role,
    string? PersonToken = null,  // For clients - links to PersonalInfoVault
    Guid? AgentId = null,        // For agents - from AgentsAppInitializer
    Guid? UserId = null);        // For logged-in agents - RecipentUserId in Communication

public enum UserRole
{
    Client = 0,
    Agent = 1,
    Supervisor = 2,  // Maps to AgentPosition.Supervisor
    Admin = 3
}

/// <summary>
/// Demo users based on existing data in the system.
/// Agents: from AgentsAppInitializer (Tickets service)
/// Clients: from DemoPersonalInfoInitializer (PersonalInfoVault service)
/// </summary>
public static class DemoUsers
{
    // Clients - PersonToken will be generated on first run by PersonalInfoVault
    public static readonly UserContext Client1 = new(
        Email: "jan.klient@example.com",
        Role: UserRole.Client,
        PersonToken: "demo-client-jan");

    public static readonly UserContext Client2 = new(
        Email: "anna.klient@example.com",
        Role: UserRole.Client,
        PersonToken: "demo-client-anna");

    // Agents - match GUIDs from AgentsAppInitializer
    public static readonly UserContext Supervisor = new(
        Email: "boguslaw.zlotowa@firma.pl",
        Role: UserRole.Supervisor,
        AgentId: new Guid("00000000-0000-0000-0000-000000000001"),
        UserId: new Guid("00000000-0000-0000-0000-000000000001"));

    public static readonly UserContext Agent1 = new(
        Email: "ziemowit.pedziwiatr@firma.pl",
        Role: UserRole.Agent,
        AgentId: new Guid("00000000-0000-0000-0000-000000000002"),
        UserId: new Guid("00000000-0000-0000-0000-000000000002"));

    public static readonly UserContext Agent2 = new(
        Email: "kunegunda.smieszek@firma.pl",
        Role: UserRole.Agent,
        AgentId: new Guid("00000000-0000-0000-0000-000000000003"),
        UserId: new Guid("00000000-0000-0000-0000-000000000003"));

    public static IReadOnlyList<UserContext> All => new[]
    {
        Client1,
        Client2,
        Supervisor,
        Agent1,
        Agent2
    };

    public static IReadOnlyList<UserContext> Clients => new[] { Client1, Client2 };
    public static IReadOnlyList<UserContext> Agents => new[] { Supervisor, Agent1, Agent2 };

    public static UserContext? GetByEmail(string email)
        => All.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public static UserContext? GetByPersonToken(string personToken)
        => Clients.FirstOrDefault(u => u.PersonToken == personToken);

    public static UserContext? GetByAgentId(Guid agentId)
        => Agents.FirstOrDefault(u => u.AgentId == agentId);
}

public static class UserRoleExtensions
{
    public static bool CanAccessTool(this UserRole role, string toolName)
    {
        return toolName switch
        {
            // Client tools - everyone can use
            "check_inquiry_status" => true,
            "list_my_inquiries" => true,

            // Agent tools - Agent, Supervisor, Admin
            "list_tickets" => role >= UserRole.Agent,
            "get_ticket" => role >= UserRole.Agent,
            "get_notes" => role >= UserRole.Agent,
            "add_note" => role >= UserRole.Agent,
            "resolve_ticket" => role >= UserRole.Agent,

            // Supervisor tools - Supervisor, Admin
            "assign_agent" => role >= UserRole.Supervisor,
            "list_agents" => role >= UserRole.Supervisor,
            "qualify_ticket" => role >= UserRole.Supervisor,

            // Admin tools - Admin only
            "delete_ticket" => role >= UserRole.Admin,
            "audit_logs" => role >= UserRole.Admin,

            // Default: deny unknown tools
            _ => false
        };
    }
}
