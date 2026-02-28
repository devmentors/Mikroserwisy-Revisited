# Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────┐
│                      RabbitMQ                           │
│  (All service messages flow through here)               │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼ (observe)
              ┌───────────────────────┐
              │   System Metrics      │
              │       Service         │
              └───────────┬───────────┘
                          │
                          ▼ (SignalR)
              ┌───────────────────────┐
              │     Frontend          │
              │   (Live Dashboard)    │
              └───────────────────────┘
```

## Components

### LiveMetricsHub
SignalR hub that manages client connections and broadcasts metrics.

### LiveMetricsPullService
Background service that:
1. Polls metrics data
2. Aggregates values
3. Pushes updates to connected clients

## Data Flow

1. Services publish messages to RabbitMQ
2. SystemMetrics observes message flow
3. Background service aggregates metrics
4. SignalR broadcasts updates to clients
5. Frontend displays real-time dashboard

## Metrics Tracked

- **Tickets**: Created, qualified, resolved, blocked
- **Inquiries**: Submitted, processed
- **SLA**: Breaches, average response time
- **Agents**: Active, assignments
- **System**: Message throughput, latency
