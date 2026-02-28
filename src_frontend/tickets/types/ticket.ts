import { TicketCategory, SeverityLevel, TicketStatus, TicketType } from '@/app/types/enums'

export interface Ticket {
  id: string;
  title: string;
  email: string;
  status: TicketStatus;
  createdAt: Date;
  deadline: Date | null;
  description: string;
  descriptionTranslated: string | null;
  severityLevel?: SeverityLevel | null;
  category: TicketCategory;
  type: TicketType;
  agentId?: string | null;
  resolution?: string | null;
}