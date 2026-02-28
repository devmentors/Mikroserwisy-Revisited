export const MESSAGE_TYPES = [
    "InquirySubmitted",
    "TicketCreated",
    "TicketQualified",
    "AgentAssignedToTicket",
    "TicketBlocked",
    "TicketResolved",
    "RequestTranslationV1",
    "RequestTranslationV2",
    "TranslationCompleted",
    "TranslationSkipped",
    "DeadlinesCalculated",
] as const;

export type MessageType = typeof MESSAGE_TYPES[number]; 