export interface Message {
    id: string;
    title: string;
    preview: string;
    content: string;
    timestamp: string;
    isRead: boolean;
    senderDisplayName?: string;
    senderEmployeeId?: string;
  }