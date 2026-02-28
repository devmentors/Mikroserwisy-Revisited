import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Label } from "@/components/ui/label"
import { SeverityLevel, TicketType } from "@/app/types/enums"
import { ticketSeverityTranslations, ticketTypeTranslations } from "@/app/lib/translations"
import { useState } from "react"
import { Ticket } from "@/types/ticket"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import api from '@/lib/axios'
import { toast } from "sonner"

interface TicketSettingsDialogProps {
  ticket: Ticket
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface QualifyPayload {
  ticketId: string
  ticketType: TicketType
  severityLevel: SeverityLevel
}

export function TicketSettingsDialog({ ticket, open, onOpenChange }: TicketSettingsDialogProps) {
  const queryClient = useQueryClient()
  const [severity, setSeverity] = useState<SeverityLevel>(ticket.severityLevel as SeverityLevel)
  const [type, setType] = useState<TicketType>(ticket.type || TicketType.Question)

  const qualifyMutation = useMutation({
    mutationFn: async (data: QualifyPayload) => {
      const response = await api.post(`/tickets/${data.ticketId}/qualify`, data)
      return response.data
    },
    onSuccess: async (data) => {
      onOpenChange(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['tickets'] }),
        queryClient.invalidateQueries({ queryKey: ['ticket', ticket.id] })
      ]);
      toast.success("Zgłoszenie zostało zakwalifikowane")
    },
    onError: (error) => {
      toast.error("Wystąpił błąd podczas kwalifikacji zgłoszenia")
      console.error("Qualification error:", error)
    }
  })

  const handleSave = () => {
    try {
      qualifyMutation.mutate({
        ticketId: ticket.id.toString(),
        ticketType: type,
        severityLevel: severity
      })
    } catch (error) {
      console.error("Handle save error:", error)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        {/* Your existing children */}
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Kwalifikacja zgłoszenia</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label>Priorytet</Label>
            <Select 
              value={(severity ?? "Nie określono").toString()} 
              onValueChange={(value) => setSeverity(value as unknown as SeverityLevel)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Wybierz priorytet" />
              </SelectTrigger>
              <SelectContent>
                {Object.values(SeverityLevel).map((level) => (
                  <SelectItem key={level} value={level.toString()}>
                    {ticketSeverityTranslations[level as keyof typeof ticketSeverityTranslations]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Typ zgłoszenia</Label>
            <Select 
              value={(type ?? TicketType.Question).toString()}
              onValueChange={(value) => setType(value as unknown as TicketType)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Wybierz typ" />
              </SelectTrigger>
              <SelectContent>
                {Object.values(TicketType)
                  .filter(ticketType => ticketType !== TicketType.Unknown)
                  .map((ticketType) => (
                    <SelectItem key={ticketType} value={ticketType.toString()}>
                      {ticketTypeTranslations[ticketType as keyof typeof ticketTypeTranslations]}
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Anuluj
          </Button>
          <Button 
            onClick={handleSave} 
            disabled={qualifyMutation.isPending}
          >
            {qualifyMutation.isPending ? "Zapisywanie..." : "Zapisz"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
} 