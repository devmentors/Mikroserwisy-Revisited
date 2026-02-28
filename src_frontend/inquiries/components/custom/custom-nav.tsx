"use client";

import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faEnvelope } from "@fortawesome/free-solid-svg-icons";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import {
    NavigationMenu,
    NavigationMenuItem,
    NavigationMenuLink,
    NavigationMenuList,
  } from "@/components/ui/navigation-menu";
import { MessageList, useUnreadMessages } from "../messages/message-list";
import { UserSelector } from "./user-selector";
import { useUserStore } from "@/store/use-user-store";

const menuItemStyle = "group inline-flex h-9 w-max items-center justify-center rounded-md px-4 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-accent/50 data-[state=open]:bg-accent/50";

export function CustomNavigationMenu() {

  const { data: unreadCount } = useUnreadMessages();
  const { selectedUser, setSelectedUser } = useUserStore();

  return (
    <div className="flex items-center justify-between w-full">
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
            <Link href="/inquiries-list" legacyBehavior passHref>
              <NavigationMenuLink
                className={menuItemStyle}
                active={true}
              >
                Zgłoszenia
              </NavigationMenuLink>
            </Link>
          </NavigationMenuItem>
          <NavigationMenuItem className="pr-2">
            <Link href="http://localhost:21001" legacyBehavior passHref>
              <NavigationMenuLink className={menuItemStyle}>
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
        <UserSelector selectedUser={selectedUser} onUserChange={setSelectedUser} allowedRoles={["client"]} />
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
            <MessageList />
          </SheetContent>
        </Sheet>
      </div>
    </div>
  );
} 