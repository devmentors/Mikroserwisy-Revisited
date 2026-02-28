import { useQuery } from "@tanstack/react-query";
import { Alert } from "@/types/alert";
import { API } from '@/lib/api-config';

export function useAlerts(onlyUnread: boolean = false) {
  const { data, isLoading, refetch } = useQuery<Alert[]>({
    queryKey: ["alerts", onlyUnread],
    queryFn: async () => {
      const response = await fetch(`${API.communication}/alerts${onlyUnread ? '?onlyUnread=true' : ''}`);
      if (!response.ok) throw new Error("Failed to fetch alerts");
      return response.json();
    },
  });

  const changeIsRead = async (id: string, isRead: boolean) => {
    await fetch(`${API.communication}/alerts/${id}?isRead=${isRead}`, {
      method: 'PUT'
    });
    refetch();
  };

  return { data, isLoading, refetch, changeIsRead };
}