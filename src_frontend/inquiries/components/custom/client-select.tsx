"use client"

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import type { Client } from "@/lib/clients"
import { cn } from "@/lib/utils"

interface ClientSelectProps {
  clients: Client[]
  value?: string
  onChange: (personToken: string) => void
  disabled?: boolean
  size?: "compact" | "large"
  placeholder?: string
}

export function ClientSelect({
  clients,
  value,
  onChange,
  disabled,
  size = "compact",
  placeholder = "Wybierz klienta"
}: ClientSelectProps) {
  const selectedClient = clients.find(c => c.personToken === value)

  const sizeStyles = {
    compact: {
      trigger: "w-[280px]",
      triggerAvatar: "h-6 w-6",
      avatar: "h-6 w-6",
      text: "text-sm",
      email: "text-xs",
      item: "py-2",
    },
    large: {
      trigger: "w-[320px]",
      triggerAvatar: "h-7 w-7",
      avatar: "h-10 w-10",
      text: "text-base",
      email: "text-sm",
      item: "py-3",
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
          {selectedClient && (
            <div className="flex items-center justify-center gap-3">
              <Avatar className={cn(styles.triggerAvatar, "flex-shrink-0")}>
                <AvatarFallback className={styles.text}>
                  {selectedClient.name.split(' ').map(n => n[0]).join('')}
                </AvatarFallback>
              </Avatar>
              <span className={cn(styles.text, "leading-none my-auto")}>
                {selectedClient.name}
              </span>
              <span className={cn(styles.email, "text-muted-foreground ml-auto leading-none my-auto")}>
                {selectedClient.email}
              </span>
            </div>
          )}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {clients.map((client) => (
          <SelectItem
            key={client.personToken}
            value={client.personToken}
            className={cn("flex items-center gap-2", styles.item)}
          >
            <div className="flex items-center gap-3 w-full">
              <Avatar className={styles.avatar}>
                <AvatarFallback className={styles.text}>
                  {client.name.split(' ').map(n => n[0]).join('')}
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col">
                <span className={styles.text}>{client.name}</span>
                <span className={cn(styles.email, "text-muted-foreground")}>{client.email}</span>
              </div>
            </div>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
