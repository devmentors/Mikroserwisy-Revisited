export enum AnonymizationStatus {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Failed = "Failed"
}

export interface ServiceAnonymizationStatus {
  serviceName: string;
  completed: boolean;
  completedAt: string | null;
  errorMessage: string | null;
}

export interface AnonymizationRequest {
  id: string;
  personToken: string;
  requestedByEmail: string;
  status: AnonymizationStatus;
  createdAt: string;
  completedAt: string | null;
  serviceStatuses: ServiceAnonymizationStatus[];
}

export interface CreateAnonymizationRequest {
  email: string;
}

export const anonymizationStatusLabels: Record<AnonymizationStatus, string> = {
  [AnonymizationStatus.Pending]: "Oczekuje",
  [AnonymizationStatus.InProgress]: "W trakcie",
  [AnonymizationStatus.Completed]: "Zakonczone",
  [AnonymizationStatus.Failed]: "Nieudane"
};

export const anonymizationStatusColors: Record<AnonymizationStatus, string> = {
  [AnonymizationStatus.Pending]: "bg-yellow-500",
  [AnonymizationStatus.InProgress]: "bg-blue-500",
  [AnonymizationStatus.Completed]: "bg-green-500",
  [AnonymizationStatus.Failed]: "bg-red-500"
};
