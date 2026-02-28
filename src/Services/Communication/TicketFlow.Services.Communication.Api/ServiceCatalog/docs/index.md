# Communication Service

Handles messaging, alerts, and notifications.

## Overview

The Communication Service manages all outbound communication in the system, including alerts for agents and messages for customers.

## Key Features

- Alert management (read/unread status)
- Customer messaging
- Support for logged-in and anonymous users
- Integration with ticket events

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5148 | HTTP port |
| Database | PostgreSQL | Persistence layer |
| Message Broker | RabbitMQ | Async messaging |

## Team

**Owner:** ticketflow-team-3 (Operations Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
