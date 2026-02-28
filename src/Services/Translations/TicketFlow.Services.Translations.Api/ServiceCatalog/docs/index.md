# Translations Service

Provides translation services for multi-language support.

## Overview

The Translations Service handles text translation for customer inquiries submitted in non-English languages. It supports both synchronous HTTP and asynchronous RabbitMQ integration.

## Key Features

- Synchronous translation via REST API
- Asynchronous translation via RabbitMQ
- Support for multiple language versions (V1, V2)
- Skip translation for already-English content

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 5200 | HTTP port |
| Message Broker | RabbitMQ | Async messaging |

## Team

**Owner:** ticketflow-team-2 (Platform Team)

## Quick Links

- [API Reference](api.md)
- [Architecture](architecture.md)
