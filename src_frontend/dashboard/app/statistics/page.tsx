'use client'

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { API } from "@/lib/api-config";
import { useDebounce } from "@/hooks/useDebounce";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  faCheckCircle,
  faExclamationTriangle,
  faTimesCircle,
  faClock,
  faCircle,
  faCircleExclamation,
  faFire,
  faNetworkWired,
  faDatabase,
  faSearch
} from "@fortawesome/free-solid-svg-icons";

interface TimeRemaining {
  days: number;
  hours: number;
  minutes: number;
  totalMinutes: number;
  isOverdue: boolean;
}

interface SlaStatus {
  deadlineDateUtc: string;
  deadlineMet: boolean | null;
  serviceCompleted: boolean;
  timeRemaining: TimeRemaining | null;
  slaState: string;
}

interface AgentInfo {
  id: string;
  fullName: string;
  position: string;
  avatarUrl: string;
}

interface TicketWithSla {
  id: string;
  name: string;
  email: string;
  title: string;
  category: string;
  status: string;
  severityLevel: string | null;
  createdAt: string;
  agent: AgentInfo | null;
  slaStatus: SlaStatus | null;
}

interface DashboardSummary {
  totalTickets: number;
  openTickets: number;
  breachedSla: number;
  atRiskSla: number;
  onTrackSla: number;
  completedOnTime: number;
  completedLate: number;
}

interface DashboardData {
  tickets: TicketWithSla[];
  summary: DashboardSummary;
}

// Ticket entry from Scatter & Gather search
interface TicketEntryDto {
  id: string;
  name: string;
  email: string;
  title: string;
  description: string;
  descriptionTranslated: string | null;
  category: string;
  status: string;
  createdAt: string;
  severityLevel: string | null;
  agentId: string | null;
  languageCode: string;
  type: string | null;
  deadline: string | null;
  resolution: string | null;
}

// Aggregation Service types
interface TicketProjection {
  id: string;
  inquiryId: string;
  name: string;
  email: string;
  title: string;
  description: string;
  category: string;
  languageCode: string;
  status: string;
  severityLevel: string | null;
  createdAt: string;
  updatedAt: string | null;
  agentId: string | null;
  agentName: string | null;
  agentAvatarUrl: string | null;
  slaDeadlineUtc: string | null;
  slaBreached: boolean | null;
  slaServiceCompleted: boolean;
  version: number;
}

interface AggregationStatistics {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  breachedSla: number;
  onTrackSla: number;
  unassignedTickets: number;
}

type DataSource = 'scatter-gather' | 'aggregation';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const severityConfig: Record<string, { label: string; variant: "default" | "secondary" | "destructive" | "outline"; icon: any }> = {
  Low: { label: "Niski", variant: "secondary", icon: faCircle },
  Medium: { label: "Sredni", variant: "default", icon: faExclamationTriangle },
  High: { label: "Wysoki", variant: "destructive", icon: faCircleExclamation },
  Critical: { label: "Krytyczny", variant: "destructive", icon: faFire },
};

const statusTranslations: Record<string, string> = {
  Open: "Otwarty",
  InProgress: "W trakcie",
  Pending: "Oczekujacy",
  Resolved: "Rozwiazany",
  Closed: "Zamkniety",
  New: "Nowy",
  Assigned: "Przypisany",
  BeforeQualification: "Do kwalifikacji",
  Qualified: "Zakwalifikowany",
  InReview: "W przeglądzie",
};

function PriorityBadge({ severity }: { severity: string | null }) {
  if (!severity) {
    return <span className="text-muted-foreground text-sm">Nie okreslono</span>;
  }

  const config = severityConfig[severity];
  if (!config) {
    return <span className="text-muted-foreground text-sm">{severity}</span>;
  }

  return (
    <Badge variant={config.variant}>
      <FontAwesomeIcon icon={config.icon} className="mr-2" />
      {config.label}
    </Badge>
  );
}

function SlaStateBadge({ state }: { state: string }) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const config: Record<string, { className: string; label: string; icon: any }> = {
    OnTrack: { className: "bg-green-100 text-green-800", label: "Na czas", icon: faCheckCircle },
    AtRisk: { className: "bg-yellow-100 text-yellow-800", label: "Zagrozone", icon: faExclamationTriangle },
    Breached: { className: "bg-red-100 text-red-800", label: "Przekroczone!", icon: faTimesCircle },
    CompletedOnTime: { className: "bg-green-100 text-green-800", label: "Zakonczone OK", icon: faCheckCircle },
    CompletedLate: { className: "bg-orange-100 text-orange-800", label: "Zakonczone pozno", icon: faClock },
  };

  const cfg = config[state] || { className: "bg-gray-100 text-gray-800", label: state, icon: faClock };

  return (
    <span className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-full text-xs font-medium ${cfg.className}`}>
      <FontAwesomeIcon icon={cfg.icon} className="text-[0.7rem]" />
      {cfg.label}
    </span>
  );
}

export default function DashboardPage() {
  const [dataSource, setDataSource] = useState<DataSource>('scatter-gather');
  const [searchQuery, setSearchQuery] = useState('');
  const debouncedSearch = useDebounce(searchQuery, 300);

  // Search queries
  const aggregationSearchQuery = useQuery<TicketProjection[]>({
    queryKey: ["search", "aggregation", debouncedSearch],
    queryFn: async () => {
      const response = await axios.get(`${API.aggregationSearch}?query=${encodeURIComponent(debouncedSearch)}`);
      return response.data;
    },
    enabled: dataSource === 'aggregation' && debouncedSearch.length >= 2,
  });

  const scatterGatherSearchQuery = useQuery<{ data: TicketEntryDto[]; totalCount: number }>({
    queryKey: ["search", "scatter-gather", debouncedSearch],
    queryFn: async () => {
      const response = await axios.get(`${API.dashboardSearch}?query=${encodeURIComponent(debouncedSearch)}`);
      return response.data;
    },
    enabled: dataSource === 'scatter-gather' && debouncedSearch.length >= 2,
  });

  const isSearchActive = debouncedSearch.length >= 2;

  // Scatter & Gather query
  const scatterGatherQuery = useQuery<DashboardData>({
    queryKey: ["dashboard", "scatter-gather"],
    queryFn: async () => {
      const response = await axios.get(API.dashboard);
      return response.data;
    },
    refetchInterval: 10000,
    enabled: dataSource === 'scatter-gather',
  });

  // Aggregation Service queries
  const aggregationProjectionsQuery = useQuery<TicketProjection[]>({
    queryKey: ["dashboard", "aggregation-projections"],
    queryFn: async () => {
      const response = await axios.get(API.aggregationProjections);
      return response.data;
    },
    refetchInterval: 10000,
    enabled: dataSource === 'aggregation',
  });

  const aggregationStatisticsQuery = useQuery<AggregationStatistics>({
    queryKey: ["dashboard", "aggregation-statistics"],
    queryFn: async () => {
      const response = await axios.get(API.aggregationStatistics);
      return response.data;
    },
    refetchInterval: 10000,
    enabled: dataSource === 'aggregation',
  });

  const isLoading = dataSource === 'scatter-gather'
    ? scatterGatherQuery.isLoading
    : aggregationProjectionsQuery.isLoading || aggregationStatisticsQuery.isLoading;

  const error = dataSource === 'scatter-gather'
    ? scatterGatherQuery.error
    : aggregationProjectionsQuery.error || aggregationStatisticsQuery.error;

  // Helper function to transform projections to TicketWithSla
  const transformProjectionToTicket = (p: TicketProjection): TicketWithSla => ({
    id: p.id,
    name: p.name,
    email: p.email,
    title: p.title,
    category: p.category,
    status: p.status,
    severityLevel: p.severityLevel,
    createdAt: p.createdAt,
    agent: p.agentId ? {
      id: p.agentId,
      fullName: p.agentName || 'Unknown',
      position: '',
      avatarUrl: p.agentAvatarUrl || '',
    } : null,
    slaStatus: p.slaDeadlineUtc ? {
      deadlineDateUtc: p.slaDeadlineUtc,
      deadlineMet: p.slaBreached === false,
      serviceCompleted: p.slaServiceCompleted,
      timeRemaining: null,
      slaState: p.slaServiceCompleted
        ? (p.slaBreached ? 'CompletedLate' : 'CompletedOnTime')
        : (p.slaBreached ? 'Breached' : 'OnTrack'),
    } : null,
  });

  // Helper function to transform TicketEntryDto to TicketWithSla
  const transformTicketEntryToTicket = (t: TicketEntryDto): TicketWithSla => ({
    id: t.id,
    name: t.name,
    email: t.email,
    title: t.title,
    category: t.category,
    status: t.status,
    severityLevel: t.severityLevel,
    createdAt: t.createdAt,
    agent: null, // Search results don't include agent details
    slaStatus: t.deadline ? {
      deadlineDateUtc: t.deadline,
      deadlineMet: null,
      serviceCompleted: false,
      timeRemaining: null,
      slaState: 'OnTrack',
    } : null,
  });

  // Transform aggregation data to match the display format
  const getDisplayData = (): { tickets: TicketWithSla[], summary: DashboardSummary } | null => {
    // Handle search results
    if (isSearchActive) {
      if (dataSource === 'aggregation' && aggregationSearchQuery.data) {
        const tickets = aggregationSearchQuery.data.map(transformProjectionToTicket);
        return {
          tickets,
          summary: { totalTickets: tickets.length, openTickets: 0, breachedSla: 0, atRiskSla: 0, onTrackSla: 0, completedOnTime: 0, completedLate: 0 },
        };
      }
      if (dataSource === 'scatter-gather' && scatterGatherSearchQuery.data) {
        const tickets = scatterGatherSearchQuery.data.data.map(transformTicketEntryToTicket);
        return {
          tickets,
          summary: { totalTickets: tickets.length, openTickets: 0, breachedSla: 0, atRiskSla: 0, onTrackSla: 0, completedOnTime: 0, completedLate: 0 },
        };
      }
      return null;
    }

    // Normal data display
    if (dataSource === 'scatter-gather') {
      return scatterGatherQuery.data || null;
    }

    if (!aggregationProjectionsQuery.data || !aggregationStatisticsQuery.data) {
      return null;
    }

    const projections = aggregationProjectionsQuery.data;
    const stats = aggregationStatisticsQuery.data;

    const tickets: TicketWithSla[] = projections.map(transformProjectionToTicket);

    const summary: DashboardSummary = {
      totalTickets: stats.totalTickets,
      openTickets: stats.openTickets,
      breachedSla: stats.breachedSla,
      atRiskSla: 0,
      onTrackSla: stats.onTrackSla,
      completedOnTime: stats.resolvedTickets - stats.breachedSla,
      completedLate: stats.breachedSla,
    };

    return { tickets, summary };
  };

  const isSearchLoading = isSearchActive && (
    dataSource === 'aggregation' ? aggregationSearchQuery.isLoading : scatterGatherSearchQuery.isLoading
  );

  const displayData = getDisplayData();

  const renderToggle = () => (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Zrodlo danych</CardTitle>
        <CardDescription>
          Wybierz podejscie do pobierania danych
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center gap-6">
          <div
            className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${
              dataSource === 'scatter-gather'
                ? 'bg-primary/10 border-2 border-primary'
                : 'bg-muted hover:bg-muted/80'
            }`}
            onClick={() => setDataSource('scatter-gather')}
          >
            <FontAwesomeIcon icon={faNetworkWired} className={`text-xl ${dataSource === 'scatter-gather' ? 'text-primary' : 'text-muted-foreground'}`} />
            <div>
              <div className={`font-medium ${dataSource === 'scatter-gather' ? 'text-primary' : ''}`}>
                Scatter & Gather
              </div>
              <div className="text-xs text-muted-foreground">
                Pobiera dane on-demand z Tickets + SLA
              </div>
            </div>
          </div>
          <div
            className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${
              dataSource === 'aggregation'
                ? 'bg-primary/10 border-2 border-primary'
                : 'bg-muted hover:bg-muted/80'
            }`}
            onClick={() => setDataSource('aggregation')}
          >
            <FontAwesomeIcon icon={faDatabase} className={`text-xl ${dataSource === 'aggregation' ? 'text-primary' : 'text-muted-foreground'}`} />
            <div>
              <div className={`font-medium ${dataSource === 'aggregation' ? 'text-primary' : ''}`}>
                Event-Driven Projections
              </div>
              <div className="text-xs text-muted-foreground">
                Pre-agregowane dane z Aggregation Service
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderSearchInput = () => (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-lg flex items-center gap-2">
          <FontAwesomeIcon icon={faSearch} />
          Szukaj klienta
        </CardTitle>
        <CardDescription>
          Wyszukaj tickety po imieniu lub adresie email klienta
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="relative">
          <Input
            placeholder="Wpisz imie, nazwisko lub email (min. 2 znaki)..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
          <FontAwesomeIcon
            icon={faSearch}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
          />
          {isSearchLoading && (
            <div className="absolute right-3 top-1/2 -translate-y-1/2">
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary"></div>
            </div>
          )}
        </div>
        {isSearchActive && displayData && (
          <p className="text-sm text-muted-foreground mt-2">
            Znaleziono {displayData.tickets.length} ticket(ow) dla &quot;{debouncedSearch}&quot;
          </p>
        )}
        {isSearchActive && !displayData && !isSearchLoading && (
          <p className="text-sm text-muted-foreground mt-2">
            Brak wynikow dla &quot;{debouncedSearch}&quot;
          </p>
        )}
      </CardContent>
    </Card>
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        {renderToggle()}
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          <span className="ml-2">
            {dataSource === 'scatter-gather'
              ? 'Ladowanie danych z Tickets + SLA (Scatter & Gather)...'
              : 'Ladowanie danych z Aggregation Service (Event-Driven)...'}
          </span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        {renderToggle()}
        <div className="text-red-500 p-4">
          {dataSource === 'scatter-gather'
            ? 'Blad ladowania danych. Upewnij sie, ze BFF (port 5001), Tickets (port 5112) i SLA (port 5054) dzialaja.'
            : 'Blad ladowania danych. Upewnij sie, ze BFF (port 5001) i Aggregation Service (port 5055) dzialaja.'}
        </div>
      </div>
    );
  }

  if (!displayData) {
    return null;
  }

  const { tickets, summary } = displayData;

  return (
    <div className="space-y-6">
      {/* Data Source Toggle */}
      {renderToggle()}

      {/* Search Input */}
      {renderSearchInput()}

      {/* Summary Cards - hidden during search */}
      {!isSearchActive && (
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Wszystkie</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary.totalTickets}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Otwarte</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary.openTickets}</div>
          </CardContent>
        </Card>
        <Card className="border-green-200 bg-green-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-green-700">Na czas</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-700">{summary.onTrackSla}</div>
          </CardContent>
        </Card>
        <Card className="border-yellow-200 bg-yellow-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-yellow-700">Zagrozone</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-700">{summary.atRiskSla}</div>
          </CardContent>
        </Card>
        <Card className="border-red-200 bg-red-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-red-700">Przekroczone</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-700">{summary.breachedSla}</div>
          </CardContent>
        </Card>
        <Card className="border-green-200 bg-green-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-green-700">Zak. OK</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-700">{summary.completedOnTime}</div>
          </CardContent>
        </Card>
        <Card className="border-orange-200 bg-orange-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-orange-700">Zak. pozno</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-700">{summary.completedLate}</div>
          </CardContent>
        </Card>
      </div>
      )}

      {/* Tickets Table */}
      <Card>
        <CardHeader>
          <CardTitle>{isSearchActive ? `Wyniki wyszukiwania (${tickets.length})` : 'Tickety z informacja SLA'}</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tytul</TableHead>
                <TableHead>Klient</TableHead>
                <TableHead>Kategoria</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Priorytet</TableHead>
                <TableHead>Przypisany do</TableHead>
                <TableHead>Deadline SLA</TableHead>
                <TableHead>Pozostaly czas</TableHead>
                <TableHead>Stan SLA</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {tickets.map((ticket) => {
                const formatTimeRemaining = (tr: TimeRemaining | null): string => {
                  if (!tr) return "N/A";
                  const prefix = tr.isOverdue ? "-" : "";
                  if (tr.days >= 1) return `${prefix}${tr.days}d ${tr.hours}h`;
                  if (tr.hours >= 1) return `${prefix}${tr.hours}h ${tr.minutes}m`;
                  return `${prefix}${tr.minutes}m`;
                };

                const getTimeRemaining = () => {
                  if (!ticket.slaStatus) return "-";
                  if (ticket.slaStatus.serviceCompleted) return "N/A";
                  return formatTimeRemaining(ticket.slaStatus.timeRemaining);
                };

                return (
                  <TableRow key={ticket.id}>
                    <TableCell className="font-medium">{ticket.title}</TableCell>
                    <TableCell>{ticket.name}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{ticket.category}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{statusTranslations[ticket.status] || ticket.status}</Badge>
                    </TableCell>
                    <TableCell>
                      <PriorityBadge severity={ticket.severityLevel} />
                    </TableCell>
                    <TableCell>
                      {ticket.agent ? (
                        <div className="flex items-center gap-2">
                          <Avatar className="h-6 w-6">
                            <AvatarImage src={ticket.agent.avatarUrl} alt={ticket.agent.fullName} />
                            <AvatarFallback className="text-xs">
                              {ticket.agent.fullName.split(' ').map(n => n[0]).join('')}
                            </AvatarFallback>
                          </Avatar>
                          <span className="text-sm">{ticket.agent.fullName}</span>
                        </div>
                      ) : (
                        <span className="text-muted-foreground text-sm">Nieprzypisany</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {ticket.slaStatus
                        ? new Date(ticket.slaStatus.deadlineDateUtc).toLocaleString("pl-PL")
                        : "-"}
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{getTimeRemaining()}</span>
                    </TableCell>
                    <TableCell>
                      {ticket.slaStatus && <SlaStateBadge state={ticket.slaStatus.slaState} />}
                    </TableCell>
                  </TableRow>
                );
              })}
              {tickets.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9} className="text-center text-muted-foreground py-8">
                    Brak ticketow do wyswietlenia
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
