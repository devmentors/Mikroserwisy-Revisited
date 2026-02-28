import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { Message } from "@/types/message";
import { MessageItem } from "./message-item";
import { getMessages } from "@/services/messages";
import { useAgentStore } from "@/store/use-agent-store";

const MESSAGES_PER_PAGE = 5;

interface MessageListProps {
  showOnlyUnread?: boolean;
}

export function MessageList({ showOnlyUnread = false }: MessageListProps) {
  const [page, setPage] = useState(1);
  const { selectedAgent } = useAgentStore();

  const { 
    data, 
    isLoading, 
    error,
    isFetching,
    refetch 
  } = useQuery({
    queryKey: ['messages', page, selectedAgent?.userId, showOnlyUnread],
    queryFn: async () => {
      const response = await getMessages({ 
        page, 
        limit: MESSAGES_PER_PAGE,
        userId: selectedAgent!.userId,
        onlyUnread: showOnlyUnread
      });
      return response;
    },
    enabled: !!selectedAgent?.userId,
  });

  useEffect(() => {
    setPage(1);
    refetch();
  }, [showOnlyUnread, refetch]);

  const messages: Message[] = data?.messages || [];
  const totalMessages = messages.length;
  const totalPages = Math.ceil(totalMessages / MESSAGES_PER_PAGE);
  const unreadCount = messages.filter((msg) => !msg.isRead).length;

  return (
    <div className="mt-4 space-y-4">
      {isLoading ? (
        <p className="text-muted-foreground">Ładowanie wiadomości...</p>
      ) : error ? (
        <p className="text-destructive">Błąd podczas ładowania wiadomości</p>
      ) : messages.length === 0 ? (
        <p className="text-muted-foreground">Brak wiadomości</p>
      ) : (
        <>
          {messages.map((message: Message) => (
            <MessageItem 
              key={message.id}
              {...message}
              onReadStatusChange={() => refetch()}
            />
          ))}
        </>
      )}
    </div>
  );
}

export function useUnreadMessages() {
  const { selectedAgent } = useAgentStore();
  
  return useQuery({
    queryKey: ['messages', 'unreadCount', selectedAgent?.userId],
    queryFn: async () => {
      const response = await getMessages({ 
        page: 1, 
        limit: 100,
        userId: selectedAgent!.userId 
      });
      return response.messages.filter(msg => !msg.isRead).length;
    },
    enabled: !!selectedAgent?.userId,
  });
}       