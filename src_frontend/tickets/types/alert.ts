export interface Alert {
  id: string;
  message: string;
  createdAt: string;
  isRead: boolean;
  type: 'SLA' | 'Incydent';
} 