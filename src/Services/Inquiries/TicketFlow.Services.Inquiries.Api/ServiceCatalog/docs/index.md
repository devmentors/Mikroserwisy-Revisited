# Inquiries Service

Handles customer inquiries and their lifecycle.

## Overview

The Inquiries Service is the entry point for customer requests. It receives inquiries, optionally translates them, and forwards them to the Tickets Service for processing.

## Key Features

- Submit customer inquiries (async or sync mode)
- Pagination support for listing inquiries
- Integration with Translations Service for multi-language support
- RabbitMQ messaging for async processing

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5011 | HTTP port |
| Database | PostgreSQL | Persistence layer |
| Message Broker | RabbitMQ | Async messaging |

## Team

**Owner:** ticketflow-team-1 (Core Ticketing Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
