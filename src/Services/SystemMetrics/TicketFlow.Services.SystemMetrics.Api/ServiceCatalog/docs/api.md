# API Reference

## SignalR Hub

### Connection
```
WebSocket: ws://localhost:5231/live-metrics
```

### Subscribe to Metrics
Connect to the SignalR hub to receive real-time metrics updates.

**Client Example (JavaScript):**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5231/live-metrics")
    .build();

connection.on("ReceiveMetrics", (metrics) => {
    console.log("Metrics update:", metrics);
});

await connection.start();
```

## Metrics Payload

```json
{
  "timestamp": "2025-12-22T12:00:00Z",
  "metrics": {
    "ticketsCreated": 150,
    "ticketsResolved": 120,
    "averageResolutionTime": "2h 15m",
    "activeAgents": 12,
    "pendingInquiries": 45,
    "slaBreaches": 3
  }
}
```

## Events

### Consumed
- Observes all RabbitMQ messages for metrics calculation
- Does not publish any events (read-only observer)
