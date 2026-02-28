# Architecture

## Integration Modes

### Synchronous (HTTP)
```
Inquiries Service
        │
        ▼
POST /translations
        │
        ▼
Translations Service
        │
        ▼
Return translated text
```

### Asynchronous (RabbitMQ)
```
Inquiries Service
        │
        ▼
RequestTranslationV2 (RabbitMQ)
        │
        ▼
Translations Service
        │
        ▼
TranslationCompleted (RabbitMQ)
        │
        ▼
Tickets Service
```

## Message Versions

### V1 (Legacy)
- Basic translation request
- No language code metadata

### V2 (Current)
- Includes source language code
- Better error handling
- Supports translation skipping

## Translation Flow

1. Receive translation request
2. Detect source language (if not provided)
3. Check if translation needed
4. If English → publish `TranslationSkipped`
5. If non-English → translate and publish `TranslationCompleted`
