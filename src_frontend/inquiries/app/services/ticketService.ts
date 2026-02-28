import { API } from '@/lib/api-config';

export async function getTicketComments(ticketId: string): Promise<string> {
  const response = await fetch(`${API.tickets}/tickets/${ticketId}/client-notes`);
  
  if (!response.ok) {
    throw new Error('Failed to fetch ticket comments');
  }

  return response.text();
}

export async function addTicketComment(ticketId: string, note: string): Promise<void> {
  const response = await fetch(`${API.tickets}/tickets/${ticketId}/client-notes`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ note: note }),
  });

  if (!response.ok) {
    throw new Error('Failed to add comment');
  }
} 