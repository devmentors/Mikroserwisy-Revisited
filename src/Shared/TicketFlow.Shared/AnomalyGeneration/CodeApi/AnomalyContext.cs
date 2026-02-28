namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

public class AnomalyContextAccessor
{
    private readonly AsyncLocal<AnomalyContext> _context = new();
    
    public AnomalyContext? Get()
        => _context.Value;

    public AnomalyContext InitializeIfEmpty()
    {
        if (_context.Value is not null)
        {
            return _context.Value;
        }
        
        var ctx = new AnomalyContext(new());
        _context.Value = ctx;
        
        return ctx;
    }
}

public record AnomalyContext(HashSet<string> DetectedMessageTypes);