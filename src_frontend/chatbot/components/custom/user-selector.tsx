"use client"

import { useState, useEffect } from "react"
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
import { EyeOpenIcon } from "@radix-ui/react-icons"
import { cn } from "@/lib/utils"

export type UserRole = "client" | "agent" | "supervisor" | "admin";

export interface DemoUser {
  id: string;
  userId: string;
  name: string;
  email: string;
  role: UserRole;
  agentId?: string;
  avatarUrl?: string;
}

export const DEMO_USERS: DemoUser[] = [
  {
    id: "client-1",
    userId: "10000000-0000-0000-0000-000000000001",
    name: "Jan Klient",
    email: "jan.klient@example.com",
    role: "client",
  },
  {
    id: "client-2",
    userId: "10000000-0000-0000-0000-000000000002",
    name: "Anna Nowak",
    email: "anna.nowak@example.com",
    role: "client",
  },
  {
    id: "client-3",
    userId: "10000000-0000-0000-0000-000000000003",
    name: "Piotr Wiśniewski",
    email: "piotr.wisniewski@example.com",
    role: "client",
  },
  {
    id: "agent-ziemowit",
    userId: "00000000-0000-0000-0000-000000000002",
    name: "Ziemowit Pędziwiatr",
    email: "ziemowit@ticketflow.pl",
    role: "agent",
    agentId: "00000000-0000-0000-0000-000000000002",
    avatarUrl: "https://api.dicebear.com/9.x/notionists/svg?seed=Ziemowit&radius=50",
  },
  {
    id: "agent-kunegunda",
    userId: "00000000-0000-0000-0000-000000000003",
    name: "Kunegunda Śmieszek",
    email: "kunegunda@ticketflow.pl",
    role: "agent",
    agentId: "00000000-0000-0000-0000-000000000003",
    avatarUrl: "https://api.dicebear.com/9.x/notionists/svg?seed=Kunegunda&radius=50",
  },
  {
    id: "supervisor-boguslaw",
    userId: "00000000-0000-0000-0000-000000000001",
    name: "Bogusław Złotówa",
    email: "boguslaw@ticketflow.pl",
    role: "supervisor",
    agentId: "00000000-0000-0000-0000-000000000001",
    avatarUrl: "https://api.dicebear.com/9.x/notionists/svg?seed=Boguslaw&radius=50",
  },
  {
    id: "admin-1",
    userId: "20000000-0000-0000-0000-000000000001",
    name: "Admin Systemu",
    email: "admin@ticketflow.pl",
    role: "admin",
  },
];

const ROLE_TRANSLATIONS: Record<UserRole, string> = {
  client: "Klienci",
  agent: "Agenci",
  supervisor: "Supervisorzy",
  admin: "Administratorzy",
};

const ROLE_LABELS: Record<UserRole, string> = {
  client: "Klient",
  agent: "Agent",
  supervisor: "Supervisor",
  admin: "Admin",
};

interface UserSelectorProps {
  onUserChange: (user: DemoUser) => void;
  selectedUser?: DemoUser;
  allowedRoles?: UserRole[];
  size?: "compact" | "large";
}

export function UserSelector({
  onUserChange,
  selectedUser,
  allowedRoles,
  size = "compact"
}: UserSelectorProps) {
  const filteredUsers = allowedRoles
    ? DEMO_USERS.filter(u => allowedRoles.includes(u.role))
    : DEMO_USERS;

  const currentUser = selectedUser || filteredUsers[0];

  const groupedUsers = filteredUsers.reduce((acc, user) => {
    const group = acc.get(user.role) || [];
    group.push(user);
    acc.set(user.role, group);
    return acc;
  }, new Map<UserRole, DemoUser[]>());

  const sizeStyles = {
    compact: {
      trigger: "w-[280px]",
      triggerAvatar: "h-6 w-6",
      avatar: "h-6 w-6",
      text: "text-sm",
      role: "text-xs",
      item: "py-2",
      label: "text-xs"
    },
    large: {
      trigger: "w-[320px]",
      triggerAvatar: "h-7 w-7",
      avatar: "h-10 w-10",
      text: "text-base",
      role: "text-sm",
      item: "py-3",
      label: "text-sm"
    }
  };

  const styles = sizeStyles[size];

  const handleChange = (userId: string) => {
    const user = filteredUsers.find(u => u.id === userId);
    if (user) {
      onUserChange(user);
    }
  };

  const getInitials = (name: string) => name.split(' ').map(n => n[0]).join('');

  return (
    <Select
      value={currentUser.id}
      onValueChange={handleChange}
    >
      <SelectTrigger className={styles.trigger}>
        <SelectValue>
          <div className="flex items-center justify-center gap-3">
            <Avatar className={cn(styles.triggerAvatar, "flex-shrink-0")}>
              {currentUser.avatarUrl ? (
                <AvatarImage src={currentUser.avatarUrl} alt={currentUser.name} />
              ) : null}
              <AvatarFallback className={styles.text}>
                {currentUser.role === "admin" ? <EyeOpenIcon className="h-4 w-4" /> : getInitials(currentUser.name)}
              </AvatarFallback>
            </Avatar>
            <span className={cn(styles.text, "leading-none my-auto")}>
              {currentUser.name}
            </span>
            <span className={cn(styles.role, "text-muted-foreground ml-auto leading-none my-auto")}>
              {ROLE_LABELS[currentUser.role]}
            </span>
          </div>
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {Array.from(groupedUsers.entries()).map(([role, users]) => (
          <SelectGroup key={role}>
            <SelectLabel className={cn("px-2 py-1.5 font-semibold text-muted-foreground", styles.label)}>
              {ROLE_TRANSLATIONS[role]}
            </SelectLabel>
            {users.map((user) => (
              <SelectItem
                key={user.id}
                value={user.id}
                className={cn("flex items-center gap-2", styles.item)}
              >
                <div className="flex items-center gap-3 w-full">
                  <Avatar className={styles.avatar}>
                    {user.avatarUrl ? (
                      <AvatarImage src={user.avatarUrl} alt={user.name} />
                    ) : null}
                    <AvatarFallback className={styles.text}>
                      {user.role === "admin" ? <EyeOpenIcon className="h-4 w-4" /> : getInitials(user.name)}
                    </AvatarFallback>
                  </Avatar>
                  <span className={styles.text}>{user.name}</span>
                </div>
              </SelectItem>
            ))}
          </SelectGroup>
        ))}
      </SelectContent>
    </Select>
  );
}

export function useDemoUser(allowedRoles?: UserRole[]) {
  const filteredUsers = allowedRoles
    ? DEMO_USERS.filter(u => allowedRoles.includes(u.role))
    : DEMO_USERS;

  const [user, setUser] = useState<DemoUser>(filteredUsers[0]);

  useEffect(() => {
    const stored = localStorage.getItem("demo-user");
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        const found = filteredUsers.find((u) => u.id === parsed.id);
        if (found) {
          setUser(found);
        } else {
          setUser(filteredUsers[0]);
        }
      } catch {
      }
    }
  }, []);

  const setDemoUser = (newUser: DemoUser) => {
    setUser(newUser);
    localStorage.setItem("demo-user", JSON.stringify(newUser));
  };

  return { user, setDemoUser };
}
