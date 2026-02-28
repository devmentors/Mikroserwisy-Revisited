import { Message } from "@/app/types/message";
import { API } from '@/lib/api-config';

interface GetMessagesParams {
  page: number;
  limit: number;
  userId: string;
  onlyUnread?: boolean;
}

interface MessagesResponse {
  data: Message[];
  total: number;
}

export async function getMessages({ page, limit, userId, onlyUnread }: GetMessagesParams): Promise<MessagesResponse> {
  const url = new URL(`${API.communication}/logged-users/${userId}/messages/`);
  url.searchParams.set('page', page.toString());
  url.searchParams.set('limit', limit.toString());
  if (onlyUnread) {
    url.searchParams.set('onlyUnread', 'true');
  }

  const response = await fetch(url, {
    method: 'GET',
    cache: 'no-store'
  });

  if (!response.ok) {
    throw new Error('Failed to fetch messages');
  }
  return response.json();
}

export async function toggleMessageReadStatus(id: string, isRead: boolean): Promise<void> {  
  const response = await fetch(
    `${API.communication}/messages/${id}?isRead=${isRead}`,
    {
      method: 'PUT',
      cache: 'no-store'
    }
  );

  if (!response.ok) {
    throw new Error('Failed to update message status');
  }
} 