"use client";

import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";

export type UserRole = "client" | "agent" | "supervisor" | "admin";

export interface DemoUser {
  id: string;
  userId: string; // UUID for system identification
  name: string;
  email: string;
  role: UserRole;
  agentId?: string; // Only for agents/supervisors
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
  },
  {
    id: "agent-kunegunda",
    userId: "00000000-0000-0000-0000-000000000003",
    name: "Kunegunda Śmieszek",
    email: "kunegunda@ticketflow.pl",
    role: "agent",
    agentId: "00000000-0000-0000-0000-000000000003",
  },
  {
    id: "supervisor-boguslaw",
    userId: "00000000-0000-0000-0000-000000000001",
    name: "Bogusław Złotówa",
    email: "boguslaw@ticketflow.pl",
    role: "supervisor",
    agentId: "00000000-0000-0000-0000-000000000001",
  },
  {
    id: "admin-1",
    userId: "20000000-0000-0000-0000-000000000001",
    name: "Admin Systemu",
    email: "admin@ticketflow.pl",
    role: "admin",
  },
];

const roleLabels: Record<UserRole, string> = {
  client: "Klienci",
  agent: "Agenci",
  supervisor: "Supervisorzy",
  admin: "Administratorzy",
};

interface UserSelectorProps {
  onUserChange: (user: DemoUser) => void;
  selectedUser?: DemoUser;
  allowedRoles?: UserRole[];
  placeholder?: string;
}

export function UserSelector({
  onUserChange,
  selectedUser,
  allowedRoles,
  placeholder = "Zaloguj się jako..."
}: UserSelectorProps) {
  const filteredUsers = allowedRoles
    ? DEMO_USERS.filter(u => allowedRoles.includes(u.role))
    : DEMO_USERS;

  const currentUser = selectedUser || filteredUsers[0];

  // Group users by role
  const groupedUsers = filteredUsers.reduce((acc, user) => {
    const group = acc.get(user.role) || [];
    group.push(user);
    acc.set(user.role, group);
    return acc;
  }, new Map<UserRole, DemoUser[]>());

  const handleChange = (userId: string) => {
    const user = filteredUsers.find(u => u.id === userId);
    if (user) {
      onUserChange(user);
    }
  };

  return (
    <Select
      value={currentUser?.id}
      onValueChange={handleChange}
    >
      <SelectTrigger className="w-[240px]">
        <SelectValue placeholder={placeholder}>
          {currentUser && (
            <div className="flex items-center gap-3">
              <Avatar className="h-6 w-6 flex-shrink-0">
                <AvatarFallback className="text-xs">
                  {currentUser.name.split(' ').map(n => n[0]).join('')}
                </AvatarFallback>
              </Avatar>
              <span className="text-sm leading-none my-auto">
                {currentUser.name}
              </span>
              <span className="text-xs text-muted-foreground ml-auto leading-none my-auto">
                {roleLabels[currentUser.role].replace(/y$/, '').replace(/ci$/, 't')}
              </span>
            </div>
          )}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {Array.from(groupedUsers.entries()).map(([role, users]) => (
          <SelectGroup key={role}>
            <SelectLabel className="px-2 py-1.5 font-semibold text-muted-foreground text-xs">
              {roleLabels[role]}
            </SelectLabel>
            {users.map((user) => (
              <SelectItem
                key={user.id}
                value={user.id}
                className="py-2"
              >
                <div className="flex items-center gap-3 w-full">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback className="text-sm">
                      {user.name.split(' ').map(n => n[0]).join('')}
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-sm">{user.name}</span>
                </div>
              </SelectItem>
            ))}
          </SelectGroup>
        ))}
      </SelectContent>
    </Select>
  );
}
