namespace TicketFlow.Shared.Authorization;

public interface ICurrentUserService
{
    Task<UserContext?> GetCurrentUserAsync();
    Task SetCurrentUserAsync(UserContext user);
    Task ClearCurrentUserAsync();
}
