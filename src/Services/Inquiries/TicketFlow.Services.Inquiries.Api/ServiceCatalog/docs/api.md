# API Reference

## Endpoints

### Health Check
```
GET /
```
Returns service health status.

### List Inquiries
```
GET /inquiries?page={page}&limit={limit}
```
Returns paginated list of inquiries.

**Parameters:**
- `page` (optional): Page number
- `limit` (optional): Items per page

### Submit Inquiry (Async)
```
POST /inquiries/submit
```
Submits inquiry for async processing via RabbitMQ.

**Request Body:**
```json
{
  "title": "string",
  "content": "string",
  "email": "string"
}
```

### Submit Inquiry (Sync)
```
POST /inquiries/submit-sync
```
Submits inquiry and waits for ticket creation.

## Events

### Published
- `InquirySubmitted` - When inquiry is created
- `RequestTranslationV1` / `RequestTranslationV2` - When translation needed

### Consumed
- `TicketCreated` - Confirmation from Tickets Service
