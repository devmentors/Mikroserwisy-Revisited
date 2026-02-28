# Architecture

## Service Dependencies

```
┌─────────────────────┐
│    Communication    │
└──────────┬──────────┘
           │
           └──► Tickets Service (HTTP)
                └── Get ticket details
```

## Message Flow

### Alerts
```
Tickets/SLA Service
        │
        ▼
ProducerAgnosticAlertMessage (RabbitMQ)
        │
        ▼
Communication Service
        │
        ▼
Store Alert in DB
        │
        ▼
Available via GET /alerts
```

### Customer Messages
```
Ticket Resolved
        │
        ▼
TicketResolved Event (RabbitMQ)
        │
        ▼
Communication Service
        │
        ├── Fetch Ticket Details (HTTP)
        │
        ▼
Create Customer Message
        │
        ▼
Available via GET /messages
```

## Database Schema

- **Alerts** - System alerts for agents
- **Messages** - Customer messages
- **MessageReadStatus** - Read/unread tracking
