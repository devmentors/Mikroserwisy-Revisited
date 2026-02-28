import { InquiryCategory, InquiryStatus } from "./enums";

export interface Inquiry {
  id: string;
  name: string;
  title: string;
  email: string;
  description: string;
  category: InquiryCategory;
  status: InquiryStatus;
  createdAt: string;
  ticketId: string;
  comments: string;
} 