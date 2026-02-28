export enum TicketCategory {
  "General" = "General",
  "Technical" = "Technical",
  "Billing" = "Billing",
  "Other" = "Other",
}

export enum SeverityLevel {
  "Low" = "Low",
  "Medium" = "Medium",
  "High" = "High",
  "Critical" = "Critical",
}

export enum TicketStatus {
  "Unknown" = "Unknown",
  "WaitingForScheduledAction" = "WaitingForScheduledAction",
  "BeforeQualification" = "BeforeQualification",
  "Qualified" = "Qualified",
  "Resolved" = "Resolved",
  "Blocked" = "Blocked"
}

export enum TicketType {
  "Unknown" = "Unknown",
  "Incident" = "Incident",
  "Question" = "Question"
}