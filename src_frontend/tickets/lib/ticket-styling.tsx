import { faCircle, faCircleExclamation, faTriangleExclamation, faFire, faCheckCircle, faSpinner, faPlusCircle, faLock, faQuestionCircle } from "@fortawesome/free-solid-svg-icons"
import { SeverityLevel, TicketStatus } from '@/app/types/enums'
import { ticketStatusTranslations, ticketSeverityTranslations } from "@/app/lib/translations"

export const severityConfig = {
  Low: {
    label: ticketSeverityTranslations[SeverityLevel.Low],
    variant: "secondary",
    icon: faCircle
  },
  Medium: {
    label: ticketSeverityTranslations[SeverityLevel.Medium],
    variant: "default",
    icon: faTriangleExclamation
  },
  High: {
    label: ticketSeverityTranslations[SeverityLevel.High],
    variant: "destructive",
    icon: faCircleExclamation
  },
  Critical: {
    label: ticketSeverityTranslations[SeverityLevel.Critical],
    variant: "destructive",
    icon: faFire
  },
  null: {
    label: "Nie określono",
    variant: "secondary",
    icon: faQuestionCircle
  },
  undefined: {
    label: "Nie określono",
    variant: "secondary",
    icon: faQuestionCircle
  }
} as const;

export const statusConfig = {
  [TicketStatus.Unknown]: { 
    label: ticketStatusTranslations[TicketStatus.Unknown], 
    variant: "outline",
    icon: faQuestionCircle
  },
  [TicketStatus.WaitingForScheduledAction]: { 
    label: ticketStatusTranslations[TicketStatus.WaitingForScheduledAction], 
    variant: "outline",
    icon: faSpinner
  },
  [TicketStatus.BeforeQualification]: { 
    label: ticketStatusTranslations[TicketStatus.BeforeQualification], 
    variant: "outline",
    icon: faPlusCircle
  },
  [TicketStatus.Qualified]: { 
    label: ticketStatusTranslations[TicketStatus.Qualified], 
    variant: "outline",
    icon: faCheckCircle
  },
  [TicketStatus.Resolved]: { 
    label: ticketStatusTranslations[TicketStatus.Resolved], 
    variant: "secondary",
    icon: faCheckCircle
  },
  [TicketStatus.Blocked]: { 
    label: ticketStatusTranslations[TicketStatus.Blocked], 
    variant: "destructive",
    icon: faLock
  },
} as const; 

// Helper function to map API status to enum
function mapApiStatusToEnum(status: string): TicketStatus {
  switch (status) {
    case "BeforeQualification":
      return TicketStatus.BeforeQualification;
    case "WaitingForScheduledAction":
      return TicketStatus.WaitingForScheduledAction;
    case "Qualified":
      return TicketStatus.Qualified;
    case "Resolved":
      return TicketStatus.Resolved;
    case "Blocked":
      return TicketStatus.Blocked;
    default:
      return TicketStatus.Unknown;
  }
}

// Update the getStatusConfig function
export function getStatusConfig(status: string) {
  const enumStatus = mapApiStatusToEnum(status);
  return (
    statusConfig[enumStatus] ?? {
      label: ticketStatusTranslations[TicketStatus.Unknown],
      variant: "outline",
      icon: faQuestionCircle,
    }
  );
} 