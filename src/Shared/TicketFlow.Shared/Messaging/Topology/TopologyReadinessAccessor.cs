namespace TicketFlow.Shared.Messaging.Topology;

public class TopologyReadinessAccessor(TopologyOptions topologyOptions)
{
    private Dictionary<string, bool> _readinessMap = new();

    public void MarkTopologyProvisioningStart(string source)
    {
        _readinessMap.Add(source, false);
    }

    public void MarkTopologyProvisioningEnd(string source)
    {
        _readinessMap[source] = true;
    }
    
    public bool TopologyProvisioned
    {
        get
        {
            return topologyOptions.CreateTopology is false || _readinessMap.Values.All(x => x);
        }
    }
}