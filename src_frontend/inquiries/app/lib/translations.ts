import { InquiryCategory, InquiryStatus } from "@/app/types/enums";

export const inquiryCategoryTranslations = {
  [InquiryCategory.GENERAL]: "Ogólne",
  [InquiryCategory.TECHNICAL]: "Techniczne",
  [InquiryCategory.BILLING]: "Płatności",
  [InquiryCategory.OTHER]: "Inne",
} as const;

export const inquiryStatusTranslations = {
  [InquiryStatus.NEW]: "Nowe",
  [InquiryStatus.IN_PROGRESS]: "W trakcie",
  [InquiryStatus.RESOLVED]: "Rozwiązane",
  [InquiryStatus.CLOSED]: "Zamknięte",
} as const;