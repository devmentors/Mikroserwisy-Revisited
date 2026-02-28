import { TicketCategory, SeverityLevel, TicketStatus, TicketType } from '@/app/types/enums'

export interface Ticket {
  id: string;
  title: string;
  email: string;
  status: TicketStatus;
  createdAt: string;
  deadline: string | null;
  description: string;
  descriptionTranslated: string | null;
  severityLevel?: SeverityLevel | null;
  category: TicketCategory;
  type: TicketType;
  agentId?: string | null;
  resolution?: string | null;
  queuePosition?: number | null;
  escalatedToSupervisor?: boolean;
  escalationReason?: string | null;
  escalatedAt?: string | null;
}