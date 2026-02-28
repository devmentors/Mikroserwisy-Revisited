import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import type { Ticket } from "@/types/ticket"
import api from "@/lib/axios";
import { API } from '@/lib/api-config';
import { useDebounce } from 'use-debounce';

// Fetch tickets
async function getTickets({ page, limit }: UseTicketsParams): Promise<TicketsResponse> {
  await new Promise(resolve => setTimeout(resolve, 200));
  const response = await fetch(`${API.tickets}/tickets?page=${page}&limit=${limit}`)
  if (!response.ok) throw new Error('Nie udało się pobrać ticketów')
  const data = await response.json();
  return data;
}

interface AssignAgentPayload {
  ticketId: string;
  agentId: string;
}

interface TicketsResponse {
  data: Ticket[];
  totalCount: number;
}

interface UseTicketsParams {
  page: number;
  limit: number;
}

interface BlockTicketPayload {
  ticketId: string;
  reason: string;
}

interface UnblockTicketPayload {
  ticketId: string;
  reason: string;
}

interface ResolveTicketPayload {
  ticketId: string;
  resolution: string;
}

export function useTickets({ page, limit }: UseTicketsParams) {
  console.log('page changed:', page);
  const [debouncedPage] = useDebounce(page, 300);
  
  return useQuery({
    queryKey: ['tickets', debouncedPage],
    queryFn: () => getTickets({ page: debouncedPage, limit }),
    enabled: debouncedPage === page,
    refetchOnWindowFocus: false
  });
}

export function useAssignAgent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ ticketId, agentId }: AssignAgentPayload) => {
      const response = await api.post(`/tickets/${ticketId}/assign/${agentId}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
  });
}

export function useBlockTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ ticketId, reason }: BlockTicketPayload) => {
      const response = await api.post(`/tickets/${ticketId}/block/${reason}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
  });
}

export function useUnblockTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ ticketId, reason }: UnblockTicketPayload) => {
      const response = await api.post(`/tickets/${ticketId}/unblock/${reason}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
  });
}

export function useResolveTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ ticketId, resolution }: ResolveTicketPayload) => {
      const response = await api.post(`/tickets/${ticketId}/resolve/${resolution}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
  });
} 