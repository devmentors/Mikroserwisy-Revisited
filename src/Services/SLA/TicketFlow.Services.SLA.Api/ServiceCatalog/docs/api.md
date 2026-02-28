# API Reference

## Endpoints

### Get Deadline Reminders
```
GET /sla/{serviceType}/{serviceSourceId}/deadline-reminders
```
Returns deadline reminders for a specific service.

**Parameters:**
- `serviceType`: Type of service (e.g., "ticket")
- `serviceSourceId`: ID of the source entity

## Events

### Published
- `DeadlinesCalculated` - When deadlines are computed for a ticket
- `SLABreached` - Alert when SLA is violated

### Consumed
- `TicketCreated` - Triggers deadline calculation
- `TicketQualified` - Updates deadline based on priority
- `AgentAssignedToTicket` - Tracks assignment time
- `TicketResolved` - Stops SLA tracking

## HTTP Clients

The service calls other services via HTTP:

### Tickets Service
- `GET /tickets/{id}` - Get ticket details
- `GET /agents` - Get supervisor information

### Communication Service
- `POST /messages` - Send reminder messages
