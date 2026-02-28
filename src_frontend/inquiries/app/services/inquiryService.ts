import { Inquiry } from '@/app/types/inquiry';
import { API } from '@/lib/api-config';

interface PaginatedResponse {
  data: Inquiry[];
  totalCount: number;
}

export async function createInquiry(inquiryData: Partial<Inquiry>, userId?: string): Promise<void> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };

  if (userId) {
    headers['X-User-Id'] = userId;
  }

  const response = await fetch(`${API.inquiries}/inquiries/submit`, {
    method: 'POST',
    headers,
    body: JSON.stringify(inquiryData),
  });

  if (!response.ok) {
    throw new Error('Failed to create inquiry');
  }

  return;
}

export const getPaginatedInquiries = async (
  pageIndex: number,
  pageSize: number,
  userId?: string
): Promise<PaginatedResponse> => {
  const headers: HeadersInit = {};
  if (userId) {
    headers['X-User-Id'] = userId;
  }

  const response = await fetch(
    `${API.inquiries}/inquiries?page=${pageIndex + 1}&limit=${pageSize}`,
    { headers }
  );

  if (!response.ok) {
    throw new Error('Failed to fetch inquiries');
  }

  return await response.json();
}

export async function addInquiryComment(inquiryId: string, comment: string): Promise<void> {
  const response = await fetch(`${API.inquiries}/inquiries/${inquiryId}/comments`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ comment }),
  });

  if (!response.ok) {
    throw new Error('Failed to add comment');
  }

  return;
} 