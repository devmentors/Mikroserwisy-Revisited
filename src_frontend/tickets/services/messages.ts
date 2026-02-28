import { Message } from "@/types/message";
import { API } from '@/lib/api-config';

interface GetMessagesParams {
  page: number;
  limit: number;
  userId: string;
  onlyUnread?: boolean;
}

interface MessagesResponse {
  messages: Message[];
  total: number;
}

export async function getMessages({ page, limit, userId, onlyUnread }: GetMessagesParams): Promise<MessagesResponse> {
  try {
    const response = await fetch(
      `${API.communication}/logged-users/${userId}/messages/?_page=${page}&_limit=${limit}${onlyUnread ? '&onlyUnread=true' : ''}`, 
      { cache: 'no-store' }
    );
    
    if (!response.ok) {
      throw new Error('Failed to fetch messages');
    }
    
    const responseData = await response.json();
    
    return { messages: responseData.data, total: responseData.total };
  } catch (error) {
    console.error('Error fetching messages:', error);
    return { messages: [], total: 0 };
  }
}

export async function toggleMessageReadStatus(id: string, isRead: boolean): Promise<void> {
  console.log(`Calling API: /messages/${id}?isRead=${isRead}`);
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