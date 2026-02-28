# System Metrics Service

Collects and broadcasts real-time system metrics via SignalR.

## Overview

The System Metrics Service monitors the entire TicketFlow system by observing RabbitMQ messages and aggregating metrics. It provides real-time dashboards via SignalR WebSocket connections.

## Key Features

- Real-time metrics streaming via SignalR
- RabbitMQ message monitoring
- Metrics aggregation and calculation
- WebSocket-based live updates

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5231 | HTTP port |
| SignalR Hub | /live-metrics | WebSocket endpoint |
| Message Broker | RabbitMQ | Message observation |

## Team

**Owner:** ticketflow-team-2 (Platform Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
