# API Reference

## Endpoints

### Alerts

#### Get Alerts
```
GET /alerts?onlyUnread={onlyUnread}
```
Returns recent alerts (max 10).

#### Update Alert Status
```
PUT /alerts/{alertId}?isRead={isRead}
```
Mark alert as read/unread.

### Messages

#### Get Messages (Logged Users)
```
GET /logged-users/{userId}/messages?onlyUnread={onlyUnread}&page={page}&limit={limit}
```

#### Get Messages (Anonymous Users)
```
GET /anonymous-users/messages?onlyUnread={onlyUnread}&page={page}&limit={limit}
```

#### Update Message Status
```
PUT /messages/{messageId}?isRead={isRead}
```

#### Create Message
```
POST /messages
```

## Events

### Consumed
- `ProducerAgnosticAlertMessage` - Generic alerts from Tickets/SLA
- `TicketResolved` - Triggers customer notification

## HTTP Clients

### Tickets Service
- `GET /tickets/{id}` - Get ticket details for messages
