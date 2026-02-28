# API Reference

## Endpoints

### Health Check
```
GET /
```
Returns service health status.

### Translate Text
```
POST /translations
```
Synchronously translates text.

**Request Body:**
```json
{
  "text": "string",
  "sourceLanguage": "string",
  "targetLanguage": "string"
}
```

**Response:**
```json
{
  "translatedText": "string"
}
```

## Events

### Published
- `TranslationCompleted` - When translation is done
- `TranslationSkipped` - When translation not needed (already English)

Both events are sent to `translation-completed-exchange`.

### Consumed
- `RequestTranslationV1` - Legacy translation request
- `RequestTranslationV2` - Current translation request with language code
