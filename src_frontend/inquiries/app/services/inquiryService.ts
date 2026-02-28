import { Inquiry } from '@/app/types/inquiry';
import { API } from '@/lib/api-config';

// Demo email - in real app this would come from auth context
export const DEMO_USER_EMAIL = 'demo@ticketflow.com';

interface PaginatedResponse {
  data: Inquiry[];
  total: number;
}

interface ClientsApiListResponse {
  items: Array<{
    id: string;
    title: string;
    status: string;
    category: string;
    createdAt: string;
  }>;
  pagination: {
    page: number;
    limit: number;
    totalCount: number;
  };
}

export async function createInquiry(inquiryData: Partial<Inquiry>): Promise<void> {
  const response = await fetch(`${API.inquiries}/v2/inquiries`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(inquiryData),
  });

  if (!response.ok) {
    throw new Error('Failed to create inquiry');
  }

  return;
}

export const getPaginatedInquiries = async (pageIndex: number, pageSize: number): Promise<PaginatedResponse> => {
  const response = await fetch(
    `${API.inquiries}/v2/inquiries?email=${encodeURIComponent(DEMO_USER_EMAIL)}&page=${pageIndex + 1}&limit=${pageSize}`
  );

  if (!response.ok) {
    throw new Error('Failed to fetch inquiries');
  }

  const result: ClientsApiListResponse = await response.json();

  // Transform ClientsAPI response to match expected format
  return {
    data: result.items.map(item => ({
      id: item.id,
      title: item.title,
      status: item.status,
      category: item.category,
      createdAt: item.createdAt,
    } as Inquiry)),
    total: result.pagination.totalCount,
  };
} 