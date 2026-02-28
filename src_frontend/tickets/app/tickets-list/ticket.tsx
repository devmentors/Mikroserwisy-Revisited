"use client"

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { ColumnDef } from "@tanstack/react-table"
import { MoreHorizontal, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
  DropdownMenuGroup,
} from "@/components/ui/dropdown-menu"

import { TicketCategory, SeverityLevel, TicketStatus } from '@/app/types/enums'
import { ticketCategoryTranslations, ticketStatusTranslations, ticketSeverityTranslations } from "@/app/lib/translations";
import { useAgent, useAgents, Agent } from "@/hooks/use-agents"
import { useAssignAgent, useBlockTicket, useUnblockTicket, useResolveTicket } from "@/hooks/use-tickets"
import { toast } from "sonner"
import { Ticket } from "@/types/ticket";
import { severityConfig, statusConfig } from "@/lib/ticket-styling"
import { TicketDetails } from "@/components/ticket-details"
import { useAgentStore } from "@/store/use-agent-store"
import { TicketSettingsDialog } from "@/components/ticket-settings-dialog"
import { 
  faUserPlus, 
  faUserMinus,
  faClipboardCheck,
  faEye,
  faQuestion,
  faLock,
  faUnlock,
  faCheck
} from "@fortawesome/free-solid-svg-icons"
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip"
import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"

function groupAgentsByPosition(agents: Agent[]) {
  return agents?.reduce((groups, agent) => {
    const position = agent.position || 'Other';
    return {
      ...groups,
      [position]: [...(groups[position] || []), agent]
    };
  }, {} as Record<string, Agent[]>);
}

export const columns: ColumnDef<Ticket>[] = [
  {
    accessorKey: "title",
    header: "Tytuł",
    cell: ({ row }) => {
      const title = row.getValue("title") as string;
      const category = row.original.category as TicketCategory;
      
      const categoryLabel = ticketCategoryTranslations[category];
      
      return (
        <div className="flex items-center">
          <span className="font-medium text-muted-foreground mr-2">[{categoryLabel || category}]</span>
          <span>{title}</span>
        </div>
      );
    }
  },
  {
    accessorKey: "status",
    header: "Status",
    cell: ({ row }) => {
      const status = row.getValue("status") as TicketStatus;
      
      if (!statusConfig) {
        console.error('statusConfig is undefined');
        return <div>Loading...</div>;
      }

      const config = statusConfig[status] || {
        variant: "secondary",
        icon: faQuestion,
        label: ticketStatusTranslations[status] || "Unknown"
      };

      return (
        <div className="flex justify-start items-center">
          <Badge variant={config.variant}>
            <FontAwesomeIcon icon={config.icon} className="mr-2" />
            {config.label}
          </Badge>
        </div>
      )
    }
  },
  {
    accessorKey: "createdAt",
    header: "Data przesłania",
    cell: ({ row }) => {
      return <div>
        {(row.getValue("createdAt") as Date).toLocaleString('pl-PL', {
          dateStyle: 'short',
          timeStyle: 'short'
        })}
      </div>
    },
  },
  {
    accessorKey: "severityLevel",
    header: "Priorytet",
    cell: ({ row }) => {
      const severity = row.getValue("severityLevel") as SeverityLevel | null;
      
      if (!severity) {
        return (
          <div className="flex justify-start items-center h-full min-h-[2rem]">
            <span className="text-muted-foreground text-sm">Nie określono</span>
          </div>
        );
      }

      const config = severityConfig[severity];
      if (!config) {
        console.error(`No config found for severity: ${severity}`);
        return (
          <div className="flex justify-start items-center h-full min-h-[2rem]">
            <span className="text-muted-foreground text-sm">Błąd konfiguracji</span>
          </div>
        );
      }

      return (
        <div className="flex justify-start items-center h-full min-h-[2rem]">
          <Badge variant={config.variant}>
            <FontAwesomeIcon icon={config.icon} className="mr-2" />
            {config.label}
          </Badge>
        </div>
      );
    }
  },
  {
    accessorKey: "agentId",
    header: "Przypisany do",
    cell: ({ row }) => {
      const agentId = row.getValue("agentId") as string | undefined
      const { data: agent, isLoading } = useAgent(agentId || "")

      if (!agentId) return <span className="text-muted-foreground text-sm">Nieprzypisany</span>
      if (isLoading) return <span className="text-muted-foreground text-sm">Wczytuje...</span>
      if (!agent) return <span className="text-muted-foreground text-sm">Agent nieznaleziony</span>

      return (
        <div className="flex items-center gap-2">
          <Avatar className="h-6 w-6">
            <AvatarImage src={agent.avatarUrl} alt={`${agent.fullName}`} />
            <AvatarFallback className="text-xs">{agent.fullName[0] + agent.fullName.split(' ')?.[0]}</AvatarFallback>
          </Avatar>
          <span>{agent.fullName}</span>
          <span className="text-xs text-muted-foreground">{agent.position}</span>
        </div>
      )
    }
  },
  {
    id: "actions",
    cell: ({ row }) => {
      const ticket = row.original;
      const [dropdownOpen, setDropdownOpen] = useState(false);
      const [settingsOpen, setSettingsOpen] = useState(false);
      const [detailsOpen, setDetailsOpen] = useState(false);
      const { data: agents } = useAgents();
      const { data: currentAgent } = useAgent(ticket.agentId || "");
      const assignAgent = useAssignAgent();
      const { selectedAgent } = useAgentStore();
      const isSupervisor = selectedAgent?.position === 'Supervisor';
      const isCurrentUserAssigned = ticket.agentId === selectedAgent?.id;

      const [blockDialogOpen, setBlockDialogOpen] = useState(false);
      const [unblockDialogOpen, setUnblockDialogOpen] = useState(false);
      const [blockReason, setBlockReason] = useState("");
      const [unblockReason, setUnblockReason] = useState("");

      const blockTicket = useBlockTicket();
      const unblockTicket = useUnblockTicket();
      const resolveTicket = useResolveTicket();

      const [resolveDialogOpen, setResolveDialogOpen] = useState(false);
      const [resolution, setResolution] = useState("");

      const handleAssignAgent = async (agentId: string) => {
        try {
          if (!isSupervisor && agentId !== selectedAgent?.id) {
            toast.error('Możesz przypisać ticket tylko do siebie');
            return;
          }

          await assignAgent.mutateAsync({
            ticketId: ticket.id,
            agentId: agentId,
          })
          
          toast.success(
            `Ticket przypisany do ${agents?.find(e => e.id === agentId)?.fullName}`
          )
        } catch (error) {
          toast.error('Nie udało się przypisać ticketa')
          console.error('Error assigning ticket:', error)
        }
      }

      const canModifyTicket = () => {
        if (ticket.status === TicketStatus.BeforeQualification) return isSupervisor;
        if (!ticket.agentId) return false;
        return isSupervisor || ticket.agentId === selectedAgent?.id;
      };

      const canAssignAgent = () => {
        return ticket.status === TicketStatus.Qualified || ticket.status === TicketStatus.Blocked;
      };

      return (
        <>
          <DropdownMenu open={dropdownOpen} onOpenChange={setDropdownOpen}>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">
                <span className="sr-only">Open menu</span>
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Akcje</DropdownMenuLabel>
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="relative">
                      <DropdownMenuItem 
                        onClick={(e) => {
                          e.preventDefault();
                          setSettingsOpen(true);
                          setDropdownOpen(false);
                        }}
                        className="h-9"
                        disabled={ticket.status !== TicketStatus.BeforeQualification || !isSupervisor}
                      >
                        <FontAwesomeIcon icon={faClipboardCheck} className="mr-2 h-4 w-4" />
                        <span>Kwalifikuj ticket</span>
                      </DropdownMenuItem>
                    </div>
                  </TooltipTrigger>
                  {ticket.status !== TicketStatus.BeforeQualification && (
                    <TooltipContent>
                      <p>Ticket został już zakwalifikowany</p>
                    </TooltipContent>
                  )}
                  {!isSupervisor && (
                    <TooltipContent>
                      <p>Tylko supervisor może kwalifikować tickety</p>
                    </TooltipContent>
                  )}
                </Tooltip>

                <DropdownMenuItem 
                  onClick={(e) => {
                    e.preventDefault();
                    setDetailsOpen(true);
                    setDropdownOpen(false);
                  }}
                  className="h-9"
                >
                  <FontAwesomeIcon icon={faEye} className="mr-2 h-4 w-4" />
                  <span>Zobacz szczegóły</span>
                </DropdownMenuItem>

                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="relative">
                      {isSupervisor ? (
                        <DropdownMenuSub>
                          <DropdownMenuSubTrigger
                            disabled={!canAssignAgent()}
                          >
                            <FontAwesomeIcon icon={faUserPlus} className="mr-2 h-4 w-4" />
                            <span>Przypisz do</span>
                          </DropdownMenuSubTrigger>
                          <DropdownMenuSubContent>
                            {Object.entries(groupAgentsByPosition(agents || [])).map(([position, agents]) => (
                              <DropdownMenuGroup key={position}>
                                <DropdownMenuLabel>{position}</DropdownMenuLabel>
                                {agents.map((agent) => (
                                  <DropdownMenuItem
                                    key={agent.id}
                                    onClick={() => handleAssignAgent(agent.id)}
                                  >
                                    <Avatar className="h-6 w-6 mr-2">
                                      <AvatarImage src={agent.avatarUrl} />
                                      <AvatarFallback>{agent.fullName[0]}</AvatarFallback>
                                    </Avatar>
                                    {agent.fullName}
                                  </DropdownMenuItem>
                                ))}
                              </DropdownMenuGroup>
                            ))}
                          </DropdownMenuSubContent>
                        </DropdownMenuSub>
                      ) : (
                        <DropdownMenuItem
                          onClick={() => handleAssignAgent(selectedAgent?.id || '')}
                          disabled={!selectedAgent || !canAssignAgent()}
                        >
                          <FontAwesomeIcon icon={faUserPlus} className="mr-2 h-4 w-4" />
                          <span>Przypisz do mnie</span>
                        </DropdownMenuItem>
                      )}
                    </div>
                  </TooltipTrigger>
                  {!canAssignAgent() && (
                    <TooltipContent>
                      <p>Ticket musi być w statusie "Zakwalifikowany" lub "Zablokowany"</p>
                    </TooltipContent>
                  )}
                </Tooltip>

                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="relative">
                      <DropdownMenuItem
                        onClick={() => setBlockDialogOpen(true)}
                        className="h-9"
                        disabled={ticket.status !== TicketStatus.Qualified || !ticket.agentId || !canModifyTicket()}
                      >
                        <FontAwesomeIcon icon={faLock} className="mr-2 h-4 w-4" />
                        <span>Zablokuj ticket</span>
                      </DropdownMenuItem>
                    </div>
                  </TooltipTrigger>
                  {(!ticket.agentId && ticket.status !== TicketStatus.BeforeQualification) && (
                    <TooltipContent>
                      <p>Ticket musi mieć przypisanego agenta</p>
                    </TooltipContent>
                  )}
                  {ticket.status !== TicketStatus.Qualified && (
                    <TooltipContent>
                      <p>Ticket musi być w statusie "Zakwalifikowany"</p>
                    </TooltipContent>
                  )}
                  {!canModifyTicket() && (
                    <TooltipContent>
                      <p>Nie masz uprawnień do modyfikacji tego ticketu</p>
                    </TooltipContent>
                  )}
                </Tooltip>

                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="relative">
                      <DropdownMenuItem
                        onClick={() => setUnblockDialogOpen(true)}
                        className="h-9"
                        disabled={ticket.status !== TicketStatus.Blocked || !ticket.agentId || !canModifyTicket()}
                      >
                        <FontAwesomeIcon icon={faUnlock} className="mr-2 h-4 w-4" />
                        <span>Odblokuj ticket</span>
                      </DropdownMenuItem>
                    </div>
                  </TooltipTrigger>
                  {(!ticket.agentId && ticket.status !== TicketStatus.BeforeQualification) && (
                    <TooltipContent>
                      <p>Ticket musi mieć przypisanego agenta</p>
                    </TooltipContent>
                  )}
                  {ticket.status !== TicketStatus.Blocked && (
                    <TooltipContent>
                      <p>Ticket musi być w statusie "Zablokowany"</p>
                    </TooltipContent>
                  )}
                  {!canModifyTicket() && (
                    <TooltipContent>
                      <p>Nie masz uprawnień do modyfikacji tego ticketu</p>
                    </TooltipContent>
                  )}
                </Tooltip>

                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="relative">
                      <DropdownMenuItem
                        onClick={() => setResolveDialogOpen(true)}
                        className="h-9"
                        disabled={ticket.status !== TicketStatus.Qualified || !ticket.agentId || !canModifyTicket()}
                      >
                        <FontAwesomeIcon icon={faCheck} className="mr-2 h-4 w-4" />
                        <span>Rozwiąż ticket</span>
                      </DropdownMenuItem>
                    </div>
                  </TooltipTrigger>
                  {(!ticket.agentId && ticket.status !== TicketStatus.BeforeQualification) && (
                    <TooltipContent>
                      <p>Ticket musi mieć przypisanego agenta</p>
                    </TooltipContent>
                  )}
                  {ticket.status !== TicketStatus.Qualified && (
                    <TooltipContent>
                      <p>Ticket musi być w statusie "Zakwalifikowany"</p>
                    </TooltipContent>
                  )}
                  {!canModifyTicket() && (
                    <TooltipContent>
                      <p>Nie masz uprawnień do modyfikacji tego ticketu</p>
                    </TooltipContent>
                  )}
                </Tooltip>
              </TooltipProvider>
            </DropdownMenuContent>
          </DropdownMenu>

          <TicketSettingsDialog 
            ticket={ticket}
            open={settingsOpen}
            onOpenChange={setSettingsOpen}
          />

          <TicketDetails 
            ticket={ticket}
            open={detailsOpen}
            onOpenChange={setDetailsOpen}
          />

          <Dialog open={blockDialogOpen} onOpenChange={setBlockDialogOpen}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Podaj powód zablokowania ticketu</DialogTitle>
              </DialogHeader>
              <Input
                value={blockReason}
                onChange={(e) => setBlockReason(e.target.value)}
                placeholder="Powód zablokowania..."
              />
              <DialogFooter>
                <Button
                  variant="secondary"
                  onClick={() => setBlockDialogOpen(false)}
                >
                  Anuluj
                </Button>
                <Button
                  onClick={() => {
                    blockTicket.mutate({ ticketId: ticket.id, reason: blockReason });
                    setBlockDialogOpen(false);
                    setBlockReason("");
                  }}
                  disabled={!blockReason.trim()}
                >
                  Zablokuj
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          <Dialog open={unblockDialogOpen} onOpenChange={setUnblockDialogOpen}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Podaj powód odblokowania ticketu</DialogTitle>
              </DialogHeader>
              <Input
                value={unblockReason}
                onChange={(e) => setUnblockReason(e.target.value)}
                placeholder="Powód odblokowania..."
              />
              <DialogFooter>
                <Button
                  variant="secondary"
                  onClick={() => setUnblockDialogOpen(false)}
                >
                  Anuluj
                </Button>
                <Button
                  onClick={() => {
                    unblockTicket.mutate({ ticketId: ticket.id, reason: unblockReason });
                    setUnblockDialogOpen(false);
                    setUnblockReason("");
                  }}
                  disabled={!unblockReason.trim()}
                >
                  Odblokuj
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          <Dialog open={resolveDialogOpen} onOpenChange={setResolveDialogOpen}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Podaj rozwiązanie ticketu</DialogTitle>
              </DialogHeader>
              <Input
                value={resolution}
                onChange={(e) => setResolution(e.target.value)}
                placeholder="Rozwiązanie..."
              />
              <DialogFooter>
                <Button
                  variant="secondary"
                  onClick={() => setResolveDialogOpen(false)}
                >
                  Anuluj
                </Button>
                <Button
                  onClick={() => {
                    resolveTicket.mutate({ ticketId: ticket.id, resolution });
                    setResolveDialogOpen(false);
                    setResolution("");
                  }}
                  disabled={!resolution.trim()}
                >
                  Rozwiąż
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </>
      )
    },
  },
]