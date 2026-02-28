"use client"

import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import type { Agent } from "@/hooks/use-agents"
import { cn } from "@/lib/utils"

const POSITION_TRANSLATIONS: Record<string, string> = {
  'Agent': 'Agenci',
  'Supervisor': 'Supervisorzy',
}

interface AgentSelectProps {
  agents: Agent[]
  value?: string
  onChange: (value: string) => void
  disabled?: boolean
  size?: "compact" | "large"
  placeholder?: string
}

export function AgentSelect({ 
  agents, 
  value, 
  onChange, 
  disabled,
  size = "compact",
  placeholder = "Wybierz agenta"
}: AgentSelectProps) {
  const selectedAgent = agents.find(e => e.id === value)
  
  const groupedAgents = agents.reduce((acc, agent) => {
    const group = acc.get(agent.position) || []
    group.push(agent)
    acc.set(agent.position, group)
    return acc
  }, new Map<string, Agent[]>())

  const sizeStyles = {
    compact: {
      trigger: "w-[280px]",
      triggerAvatar: "h-6 w-6",
      avatar: "h-6 w-6",
      text: "text-sm",
      position: "text-xs",
      item: "py-2",
      label: "text-xs"
    },
    large: {
      trigger: "w-[320px]",
      triggerAvatar: "h-7 w-7",
      avatar: "h-10 w-10",
      text: "text-base",
      position: "text-sm",
      item: "py-3",
      label: "text-sm"
    }
  }

  const styles = sizeStyles[size]

  return (
    <Select
      value={value}
      onValueChange={onChange}
      disabled={disabled}
    >
      <SelectTrigger className={styles.trigger}>
        <SelectValue placeholder={placeholder}>
          {selectedAgent && (
            <div className="flex items-center justify-center gap-3">
              <Avatar className={cn(styles.triggerAvatar, "flex-shrink-0")}>
                <AvatarImage src={selectedAgent.avatarUrl} alt={selectedAgent.fullName} />
                <AvatarFallback className={styles.text}>
                  {selectedAgent.fullName.split(' ').map(n => n[0]).join('')}
                </AvatarFallback>
              </Avatar>
              <span className={cn(styles.text, "leading-none my-auto")}>
                {selectedAgent.fullName}
              </span>
              <span className={cn(styles.position, "text-muted-foreground ml-auto leading-none my-auto")}>
                {selectedAgent.position}
              </span>
            </div>
          )}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {Array.from(groupedAgents.entries()).map(([position, groupAgents]) => (
          <SelectGroup key={position}>
            <SelectLabel className={cn("px-2 py-1.5 font-semibold text-muted-foreground", styles.label)}>
              {POSITION_TRANSLATIONS[position]}
            </SelectLabel>
            {groupAgents.map((agent) => (
              <SelectItem 
                key={agent.id} 
                value={agent.id}
                className={cn("flex items-center gap-2", styles.item)}
              >
                <div className="flex items-center gap-3 w-full">
                  <Avatar className={styles.avatar}>
                    <AvatarImage src={agent.avatarUrl} alt={agent.fullName} />
                    <AvatarFallback className={styles.text}>
                      {agent.fullName.split(' ').map(n => n[0]).join('')}
                    </AvatarFallback>
                  </Avatar>
                  <span className={styles.text}>{agent.fullName}</span>
                </div>
              </SelectItem>
            ))}
          </SelectGroup>
        ))}
      </SelectContent>
    </Select>
  )
} 