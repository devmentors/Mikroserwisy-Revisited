"use client"

import { useEffect, useState } from "react"
import { useQuery } from "@tanstack/react-query";
import { Message } from "@/app/types/message";
import { MessageItem } from "./message-item";
import { getMessages } from "@/app/services/messages";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";

const MESSAGES_PER_PAGE = 5;

export function MessageList() {
  const [showOnlyUnread, setShowOnlyUnread] = useState(() => {
    const stored = localStorage.getItem('showOnlyUnread')
    return stored ? JSON.parse(stored) : false
  })

  const { 
    data, 
    isLoading, 
    error,
    refetch 
  } = useQuery({
    queryKey: ['messages', showOnlyUnread ? 'unread' : 'all'],
    queryFn: async () => {
      const response = await getMessages({ 
        page: 1, 
        limit: MESSAGES_PER_PAGE,
        onlyUnread: showOnlyUnread
      });
      return response;
    },
  });

  const handleToggleChange = (checked: boolean) => {
    setShowOnlyUnread(checked);
    localStorage.setItem('showOnlyUnread', JSON.stringify(checked));
    refetch();
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center space-x-2">
        <Switch
          checked={showOnlyUnread}
          onCheckedChange={handleToggleChange}
        />
        <Label>Pokaż tylko nieprzeczytane</Label>
      </div>
      
      {isLoading ? (
        <p className="text-muted-foreground">Ładowanie wiadomości...</p>
      ) : error ? (
        <p className="text-destructive">Błąd podczas ładowania wiadomości</p>
      ) : !data?.data || data.data.length === 0 ? (
        <p className="text-muted-foreground">Brak wiadomości</p>
      ) : (
        <>
          {data.data.map((message: Message) => (
            <MessageItem 
              key={message.id}
              {...message}
              onReadStatusChange={() => refetch()}
            />
          ))}
        </>
      )}
    </div>
  )
}

export function useUnreadMessages() {
    
  return useQuery({
    queryKey: ['messages', 'unreadCount'],
    queryFn: async () => {
      const response = await getMessages({ 
        page: 1, 
        limit: 100
      });
      return response.data.filter(msg => !msg.isRead).length;
    },
  });
}       