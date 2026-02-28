"use client"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Ticket } from "@/types/ticket"
import { SeverityLevel, TicketStatus, TicketCategory } from '@/app/types/enums'
import { ticketCategoryTranslations, ticketStatusTranslations, ticketSeverityTranslations } from "@/app/lib/translations"
import { useState } from "react"
import { severityConfig, statusConfig } from "@/lib/ticket-styling"
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { faCircle, faCircleExclamation, faTriangleExclamation, faFire } from "@fortawesome/free-solid-svg-icons"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import { Clock } from "lucide-react"

interface TicketDetailsProps {
  ticket: Ticket
  open: boolean
  onOpenChange: (open: boolean) => void
}

const formatDeadline = (deadline: Date | string | null, createdAt: Date | string) => {
  if (!deadline) return null;
  
  const deadlineDate = new Date(deadline);
  const createdAtDate = new Date(createdAt);
  
  const diff = deadlineDate.getTime() - createdAtDate.getTime();
  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);
  
  return [
    days > 0 ? `${days}d` : null,
    hours > 0 ? `${hours}h` : null,
    minutes > 0 ? `${minutes}m` : null,
    seconds > 0 ? `${seconds}s` : null
  ].filter(Boolean).join(' ');
};

export function TicketDetails({ ticket, open, onOpenChange }: TicketDetailsProps) {
  const [showTranslated, setShowTranslated] = useState(false)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        {/* Your existing children */}
      </DialogTrigger>
      <DialogContent className="max-w-2xl p-0">
        <Card className="border-0">
          <CardHeader className="p-6 pb-4 border-b">
            <CardTitle className="text-2xl font-semibold">{ticket.title}</CardTitle>
            <CardDescription className="flex items-center gap-2">
              <strong>ID:</strong> {ticket.id} • 
              <Badge variant="outline" className="flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {ticket.deadline 
                  ? `Deadline: ${formatDeadline(ticket.deadline, ticket.createdAt)}`
                  : 'Deadline nieznany'}
              </Badge>
            </CardDescription>
          </CardHeader>
          <CardContent className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <Input 
                  id="status" 
                  value={ticketStatusTranslations[ticket.status as TicketStatus]} 
                  readOnly 
                  disabled
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="severity">Priorytet</Label>
                <div className="mt-1">
                  {(() => {
                    const config = severityConfig[ticket.severityLevel as SeverityLevel] || severityConfig.Low;
                    return (
                      <Badge className="py-2" variant={config.variant}>
                        <FontAwesomeIcon icon={config.icon} className="mr-2" />
                        {config.label}
                      </Badge>
                    );
                  })()}
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="category">Kategoria</Label>
              <Input 
                id="category" 
                value={ticketCategoryTranslations[ticket.category as TicketCategory] || ticket.category} 
                readOnly 
                disabled
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email zgłaszającego</Label>
              <Input 
                id="email" 
                value={ticket.email} 
                readOnly 
                disabled
              />
            </div>

            {ticket.resolution && (
              <div className="space-y-2">
                <Label htmlFor="resolution">Rozwiązanie</Label>
                <Textarea
                  id="resolution"
                  value={ticket.resolution}
                  readOnly
                  disabled
                  className="min-h-[100px] resize-none"
                />
              </div>
            )}

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label>Opis</Label>
                <div className="flex items-center gap-2 mb-2">
                  <Switch
                    checked={showTranslated}
                    onCheckedChange={setShowTranslated}
                    disabled={!ticket.descriptionTranslated}
                  />
                  <Label className={!ticket.descriptionTranslated ? "text-muted-foreground" : ""}>
                    {ticket.descriptionTranslated ? "Pokaż tłumaczenie" : "Tłumaczenie niedostępne"}
                  </Label>
                </div>
              </div>
              <Textarea
                value={showTranslated ? (ticket.descriptionTranslated ?? ticket.description) : ticket.description}
                readOnly
                className="min-h-[150px] resize-none"
              />
            </div>
          </CardContent>
        </Card>
      </DialogContent>
    </Dialog>
  )
} 