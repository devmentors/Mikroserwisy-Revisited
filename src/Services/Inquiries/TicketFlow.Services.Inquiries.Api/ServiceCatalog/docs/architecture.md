# Architecture

## Service Dependencies

```
┌─────────────────┐
│    Inquiries    │
└────────┬────────┘
         │
         ├──► Translations Service (HTTP sync)
         │
         └──► RabbitMQ
              ├── InquirySubmitted ──► Tickets Service
              └── RequestTranslation ──► Translations Service
```

## Data Flow

1. Customer submits inquiry via REST API
2. Service checks language (feature flag)
3. If non-English, requests translation
4. Publishes `InquirySubmitted` event
5. Tickets Service creates ticket
6. Receives `TicketCreated` confirmation

## Database Schema

- **Inquiries** - Stores inquiry data
- **InquiryStatus** - Tracks processing state

## Feature Flags

- `SynchronousIntegration` - Use sync HTTP calls instead of RabbitMQ
- `TranslationEnabled` - Enable auto-translation
