import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { faEnvelope, faEnvelopeOpen } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { toggleMessageReadStatus } from "@/app/services/messages";
import { Message } from "@/app/types/message";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { User } from "lucide-react";
import { getAgent } from "@/app/services/agents";

interface MessageItemProps extends Message {
  onReadStatusChange: () => void;
}

export function MessageItem({ onReadStatusChange, ...props }: MessageItemProps) {
  const queryClient = useQueryClient();
  
  const toggleReadStatus = useMutation({
    mutationFn: () => toggleMessageReadStatus(props.id, !props.isRead),
    onSuccess: () => {
      onReadStatusChange();
      queryClient.invalidateQueries({ 
        queryKey: ['messages', 'unreadCount'] 
      });
    },
  });

  const formattedDate = new Date(props.timestamp).toLocaleString('pl-PL', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });

  let displayName;
  let agent;

  if (props.senderEmployeeId) {
    const { data: agentData } = useQuery({
      queryKey: ['agent', props.senderEmployeeId],
      queryFn: () => getAgent(props.senderEmployeeId!)
    });
    agent = agentData;
    displayName = agent 
      ? `${agent.fullName}`
      : props.senderDisplayName || 'Loading...';
  } else {
    displayName = 'SYSTEM';
  }

  return (
    <div className="relative border rounded-lg p-4 hover:bg-accent transition-colors">
      <div className="flex justify-between items-start mb-1">
        <div className="flex items-center gap-2">
          <h4 className="font-medium truncate max-w-[400px]">{props.title}</h4>
          {!props.isRead && <Badge variant="secondary">Nowa</Badge>}
        </div>
        <time className="text-sm text-muted-foreground">
          {formattedDate}
        </time>
      </div>
      <p className="text-sm text-muted-foreground mb-2">{props.preview}</p>
      <p className="text-sm mb-4">{props.content}</p>
      
      <div className="border-t pt-3 mt-3">
        <div className="flex justify-between items-center">
          <div>
            <div className="text-xs text-muted-foreground mb-1 flex items-center gap-1">
              <User className="h-3 w-3" />
              Nadawca
            </div>
            <div className="flex items-center gap-2">
              {props.senderEmployeeId && agent && (
                <Avatar className="h-8 w-8">
                  <AvatarImage src={agent.avatarUrl} alt={displayName} />
                  <AvatarFallback>{displayName.split(' ').map((n: string) => n[0]).join('')}</AvatarFallback>
                </Avatar>
              )}
              <span className="text-sm font-medium">{displayName}</span>
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => toggleReadStatus.mutate()}
            disabled={toggleReadStatus.isPending}
          >
            <FontAwesomeIcon 
              icon={props.isRead ? faEnvelopeOpen : faEnvelope} 
              className={`h-4 w-4 ${props.isRead ? 'text-muted-foreground' : 'text-primary'}`}
            />
          </Button>
        </div>
      </div>
    </div>
  );
} 