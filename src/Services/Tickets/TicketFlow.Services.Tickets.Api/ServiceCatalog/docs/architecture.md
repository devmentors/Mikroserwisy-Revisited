# Architecture

## Service Dependencies

```
                    ┌─────────────────┐
                    │     Tickets     │
                    └────────┬────────┘
                             │
    ┌────────────────────────┼────────────────────────┐
    │                        │                        │
    ▼                        ▼                        ▼
Inquiries              Translations                 SLA
(InquirySubmitted)   (TranslationCompleted)   (DeadlinesCalculated)
```

## Ticket Lifecycle

```
Created ──► Qualified ──► Assigned ──► In Progress ──► Resolved
                              │              │
                              ▼              ▼
                           Blocked ◄────► Unblocked
```

## Data Flow

1. Receives `InquirySubmitted` from Inquiries Service
2. Creates ticket with initial status
3. Publishes `TicketCreated` event
4. SLA Service calculates deadlines
5. Agent gets assigned (manual or auto)
6. Ticket progresses through lifecycle
7. Resolution triggers notifications

## Database Schema

- **Tickets** - Core ticket data
- **Agents** - Agent information
- **Users** - User data
- **ClientNotes** - Notes attached to tickets

## Partitioning

Events use `TicketId` as partition key for ordered processing.
