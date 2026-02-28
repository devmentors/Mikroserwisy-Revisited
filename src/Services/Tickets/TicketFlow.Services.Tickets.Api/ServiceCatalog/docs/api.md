# API Reference

## Endpoints

### Tickets

#### List Tickets
```
GET /tickets?agentId={agentId}&status={status}&page={page}&limit={limit}
```

#### Get Ticket
```
GET /tickets/{ticketId}
```

#### Create Ticket
```
POST /tickets
```

#### Qualify Ticket
```
POST /tickets/{ticketId}/qualify
```

#### Assign Agent
```
POST /tickets/{ticketId}/assign/{agentId}
```

#### Block Ticket
```
POST /tickets/{ticketId}/block/{reason}
```

#### Unblock Ticket
```
POST /tickets/{ticketId}/unblock/{reason}
```

#### Resolve Ticket
```
POST /tickets/{ticketId}/resolve/{resolution}
```

### Client Notes

#### Get Notes
```
GET /tickets/{ticketId}/client-notes
```

#### Add Note
```
POST /tickets/{ticketId}/client-notes
```

### Agents

#### List Agents
```
GET /agents
```

#### Get Agent
```
GET /agents/{id}
```

### Users

#### Get User
```
GET /users/{id}
```

## Events

### Published
- `TicketCreated` - When ticket is created
- `TicketQualified` - When ticket is qualified
- `AgentAssignedToTicket` - When agent assigned
- `TicketBlocked` - When ticket blocked
- `TicketResolved` - When ticket resolved
- `IncidentCreated` - Alert for incidents

### Consumed
- `InquirySubmitted` - From Inquiries Service
- `TranslationCompleted` - From Translations Service
- `DeadlinesCalculated` - From SLA Service
