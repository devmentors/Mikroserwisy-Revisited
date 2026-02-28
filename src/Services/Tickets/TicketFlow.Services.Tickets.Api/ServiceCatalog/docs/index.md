# Tickets Service

Manages ticket lifecycle, assignments, and resolutions.

## Overview

The Tickets Service is the core of the TicketFlow system. It handles the entire lifecycle of support tickets from creation to resolution.

## Key Features

- Create tickets from inquiries
- Ticket qualification and prioritization
- Agent assignment management
- Ticket blocking/unblocking
- Resolution tracking
- Client notes management

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5112 | HTTP port |
| Database | PostgreSQL | Persistence layer |
| Message Broker | RabbitMQ | Async messaging |

## Team

**Owner:** ticketflow-team-1 (Core Ticketing Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
