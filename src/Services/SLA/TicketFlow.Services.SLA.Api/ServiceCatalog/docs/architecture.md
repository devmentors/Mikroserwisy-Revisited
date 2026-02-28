# Architecture

## Service Dependencies

```
┌─────────────────┐
│       SLA       │
└────────┬────────┘
         │
         ├──► Tickets Service (HTTP)
         │    └── Get ticket details, agents
         │
         └──► Communication Service (HTTP)
              └── Send reminder messages
```

## Background Workers

### RemindersWatcher
Periodically checks for upcoming deadlines and sends reminders.

### DeadlineBreachWatcher
Monitors for SLA breaches and triggers alerts.

## SLA Calculation

```
Ticket Created
      │
      ▼
Calculate Initial Deadline (based on default SLA)
      │
      ▼
Ticket Qualified
      │
      ▼
Recalculate Deadline (based on priority)
      │
      ▼
Monitor Progress
      │
      ├──► Send Reminders (approaching deadline)
      │
      └──► Alert Breach (deadline passed)
```

## Database Schema

- **Deadlines** - Tracks ticket deadlines
- **Reminders** - Sent reminder history
- **SLAConfigurations** - SLA rules per priority

## Partitioning

Uses `TicketId` as partition key for ordered event processing.
