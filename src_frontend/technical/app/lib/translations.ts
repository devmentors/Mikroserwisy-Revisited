import { TicketCategory, TicketStatus, SeverityLevel, TicketType } from "@/app/types/enums";

export const ticketCategoryTranslations = {
  [TicketCategory.General]: "Ogólne",
  [TicketCategory.Technical]: "Techniczne",
  [TicketCategory.Billing]: "Płatności",
  [TicketCategory.Other]: "Inne",
} as const;

export const ticketStatusTranslations = {
  [TicketStatus.Unknown]: "Nieznany",
  [TicketStatus.WaitingForScheduledAction]: "Oczekujące na akcję systemu",
  [TicketStatus.BeforeQualification]: "Przed kwalifikacją",
  [TicketStatus.Qualified]: "Zakwalifikowane",
  [TicketStatus.Resolved]: "Rozwiązane",
  [TicketStatus.Blocked]: "Zablokowane",
} as const;

export const ticketSeverityTranslations = {
  [SeverityLevel.Low]: "Niski",
  [SeverityLevel.Medium]: "Średni",
  [SeverityLevel.High]: "Wysoki",
  [SeverityLevel.Critical]: "Krytyczny",
} as const; 

export const ticketTypeTranslations = {
  [TicketType.Unknown]: "Nieznany",
  [TicketType.Incident]: "Incydent",
  [TicketType.Question]: "Pytanie",
} as const;

