# SLA Service

Monitors and enforces SLA compliance for tickets.

## Overview

The SLA Service tracks ticket deadlines, sends reminders, and alerts when SLA breaches occur. It ensures timely resolution of customer issues.

## Key Features

- Calculate deadlines based on ticket priority
- Send deadline reminders to agents
- Detect and alert on SLA breaches
- Track SLA metrics per service type

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5054 | HTTP port |
| Database | PostgreSQL | Persistence layer |
| Message Broker | RabbitMQ | Async messaging |

## Team

**Owner:** ticketflow-team-3 (Operations Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
