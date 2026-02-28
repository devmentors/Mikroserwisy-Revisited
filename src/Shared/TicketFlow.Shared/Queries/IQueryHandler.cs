namespace TicketFlow.Shared.Queries;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult> where TResult : class
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}