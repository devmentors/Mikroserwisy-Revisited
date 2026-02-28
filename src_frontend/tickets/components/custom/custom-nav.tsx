"use client"

import {
    NavigationMenu,
    NavigationMenuItem,
    NavigationMenuLink,
    NavigationMenuList,
    navigationMenuTriggerStyle,
  } from "@/components/ui/navigation-menu";
import Link from "next/link";
import { AgentSelect } from "./agent-select";
import React, { useState, useEffect } from "react";
import { useAgents } from "@/hooks/use-agents";
import { useAgentStore } from '@/store/use-agent-store';
import { AlertCircle, BarChart, Clock, UserPlus, Ticket } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import { AlertList } from "@/components/alerts/alert-list";
import { ServiceStatusList } from "@/components/metrics/service-status-list";
import { useServices } from "@/hooks/use-services";
import { faEnvelope } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { MessageList, useUnreadMessages } from "@/components/messages/message-list";
import { useRouter, useSearchParams } from 'next/navigation';
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { AlertIndicator } from "@/components/alerts/alert-list";

export function CustomNavigationMenu() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [showOnlyUnread, setShowOnlyUnread] = useState(() => {
    // Initialize from localStorage if available, otherwise false
    if (typeof window !== 'undefined') {
      return localStorage.getItem('showOnlyUnread') === 'true';
    }
    return false;
  });
  const { selectedAgent, setSelectedAgent } = useAgentStore();
  const { data: agents, isLoading: isLoadingAgents } = useAgents();
  const { services, isConnected } = useServices();
  const { data: unreadCount } = useUnreadMessages();

  useEffect(() => {
    // Update localStorage when preference changes
    localStorage.setItem('showOnlyUnread', showOnlyUnread.toString());
  }, [showOnlyUnread]);

  const handleAgentChange = (agentId: string) => {
    const agent = agents?.find(agn => agn.id === agentId) ?? null;
    setSelectedAgent(agent);
  };

  const menuItemStyle = "group inline-flex h-9 w-max items-center justify-center rounded-md px-4 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-accent/50 data-[state=open]:bg-accent/50";

  return (
    <div className="flex items-center justify-between w-full gap-4 px-8">
      <div className="flex items-center gap-4">
        <div className="flex items-center pr-4">
            <img 
                src="/img/ticketflow_logo.png" 
                alt="TicketFlow Logo"
                width="124"
                height="auto"
            />
        </div>
        
        <NavigationMenu>
          <NavigationMenuList>
            <NavigationMenuItem className="pr-2">
              <Link href="http://localhost:21000" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle}>
                  Zgłoszenia
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
            <NavigationMenuItem className="pr-2">
              <Link href="/tickets-list" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle} active={true}>
                  Tickety
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
            <NavigationMenuItem className="pr-2">
              <Link href="http://localhost:21003/statistics" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle}>
                  Dashboard Managera
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
            <NavigationMenuItem className="pr-2">
              <Link href="http://localhost:21002" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle}>
                  Panel admina
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
            <NavigationMenuItem className="pr-2">
              <Link href="http://localhost:21200" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle}>
                  Chatbot
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
          </NavigationMenuList>
        </NavigationMenu>
      </div>

      <div className="flex items-center gap-2">
        <AgentSelect
          agents={agents || []}
          value={selectedAgent?.id}
          onChange={handleAgentChange}
          disabled={isLoadingAgents}
          size="large"
          placeholder="Pracuj jako..."
        />
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="ghost" size="icon" className="h-9 w-9">
              <BarChart className="h-5 w-5" />
            </Button>
          </SheetTrigger>
          <SheetContent side="right" className="sm:max-w-[800px] w-[90vw]">
            <SheetHeader>
              <SheetTitle>Status usług</SheetTitle>
            </SheetHeader>
            <div className="mt-4">
              <ServiceStatusList 
                services={services} 
                isConnected={isConnected}
              />
            </div>
          </SheetContent>
        </Sheet>
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="ghost" size="icon" className="h-9 w-9">
              <AlertIndicator />
            </Button>
          </SheetTrigger>
          <SheetContent>
            <SheetHeader>
              <SheetTitle>Alerty</SheetTitle>
            </SheetHeader>
            <div className="mt-4">
              <AlertList />
            </div>
          </SheetContent>
        </Sheet>
        
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="ghost" size="icon" className="h-9 w-9 relative">
              <FontAwesomeIcon icon={faEnvelope} className="h-5 w-5" />
              {unreadCount && unreadCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-primary text-primary-foreground text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {unreadCount}
                </span>
              )}
            </Button>
          </SheetTrigger>
          <SheetContent className="w-[95vw] sm:w-[600px] overflow-y-auto max-w-[95vw] sm:max-w-[600px]">
            <SheetHeader>
              <SheetTitle>Wiadomości</SheetTitle>
            </SheetHeader>
            <div className="flex items-center space-x-2 mb-4">
              <Switch
                id="unread-mode"
                checked={showOnlyUnread}
                onCheckedChange={setShowOnlyUnread}
              />
              <Label htmlFor="unread-mode">Pokaż tylko nieprzeczytane</Label>
            </div>
            <MessageList showOnlyUnread={showOnlyUnread} />
          </SheetContent>
        </Sheet>
      </div>
    </div>
  );
} 