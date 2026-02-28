"use client";

import Link from "next/link";
import {
  NavigationMenu,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
} from "@/components/ui/navigation-menu";

const menuItemStyle = "group inline-flex h-9 w-max items-center justify-center rounded-md px-4 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-accent/50 data-[state=open]:bg-accent/50";

export function CustomNavigationMenu() {
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
              <Link href="http://localhost:21000" legacyBehavior passHref>
                <NavigationMenuLink className={menuItemStyle}>
                  Zgloszenia
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
              <Link href="/" legacyBehavior passHref>
                <NavigationMenuLink
                  className={menuItemStyle}
                  active={true}
                >
                  Chatbot
                </NavigationMenuLink>
              </Link>
            </NavigationMenuItem>
          </NavigationMenuList>
        </NavigationMenu>
      </div>
    </div>
  );
}
