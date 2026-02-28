"use client";

import { useEffect, useState } from "react";
import { CopilotKit, useCopilotReadable } from "@copilotkit/react-core";
import { CopilotChat } from "@copilotkit/react-ui";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faComments, faCircle, faServer } from "@fortawesome/free-solid-svg-icons";
import { UserSelector, useDemoUser, type UserRole, type DemoUser } from "@/components/custom/user-selector";
import { CustomAssistantMessage } from "@/components/custom/custom-assistant-message";

const AGENT_URL = process.env.NEXT_PUBLIC_AGENT_URL || "http://localhost:8000";

interface ToolInfo {
  name: string;
  description: string;
  server: string;
}

function ConnectionStatus({ backendConnected, isLoading }: { backendConnected: boolean; isLoading: boolean }) {
  const statusColor = isLoading ? "text-yellow-500" : backendConnected ? "text-green-500" : "text-red-500";
  const statusText = isLoading ? "Laczenie..." : backendConnected ? "Polaczono" : "Rozlaczono";

  return (
    <div className="flex items-center gap-2 text-xs">
      <FontAwesomeIcon icon={faCircle} className={`${statusColor} w-2 h-2`} />
      <span className="text-muted-foreground">{statusText}</span>
    </div>
  );
}

function ChatWithUserContext({
  user,
  setDemoUser,
  tools,
  backendConnected,
  loading,
}: {
  user: DemoUser;
  setDemoUser: (u: DemoUser) => void;
  tools: ToolInfo[];
  backendConnected: boolean;
  loading: boolean;
}) {
  useCopilotReadable({
    description: "Current logged-in user information. Use this to determine which tools the user can access and how to identify them.",
    value: {
      userId: user.userId,
      userName: user.name,
      userEmail: user.email,
      userRole: user.role,
      agentId: user.agentId || null,
      allowedTools: tools.map(t => t.name),
    },
  });

  return (
    <div className="w-full h-full flex flex-col">
      <div className="mb-4 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
        <div>
          <div className="flex items-center gap-4">
            <h3 className="scroll-m-20 pb-2 text-3xl font-semibold tracking-tight first:mt-0">
              <FontAwesomeIcon className="pr-4" icon={faComments} />
              Asystent TicketFlow
            </h3>
            <ConnectionStatus backendConnected={backendConnected} isLoading={loading} />
          </div>
          <p className="text-muted-foreground">
            Porozmawiaj z asystentem AI, ktory pomoze Ci zarzadzac ticketami
          </p>
        </div>

        <div className="sm:w-80">
          <label className="text-xs text-muted-foreground mb-1 block">
            Zalogowany jako (demo):
          </label>
          <UserSelector selectedUser={user} onUserChange={setDemoUser} />

          <div className="mt-2 text-xs text-muted-foreground">
            Dostepne narzedzia: {tools.length}
            <div className="flex flex-wrap gap-1 mt-1">
              {tools.map((tool) => (
                <span
                  key={tool.name}
                  className="px-1.5 py-0.5 bg-secondary rounded text-[10px]"
                  title={tool.description}
                >
                  {tool.name}
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="flex-1 border rounded-lg overflow-hidden min-h-[500px]">
        <CopilotChat
          key={user.id}
          className="h-full"
          labels={{
            title: "TicketFlow Assistant",
            initial: getInitialMessage(user.role),
            placeholder: "Napisz wiadomosc...",
          }}
          AssistantMessage={CustomAssistantMessage}
        />
      </div>
    </div>
  );
}

export default function Home() {
  const { user, setDemoUser } = useDemoUser();
  const [tools, setTools] = useState<ToolInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [backendConnected, setBackendConnected] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setBackendConnected(false);

      try {
        const toolsResponse = await fetch(`${AGENT_URL}/tools`, {
          headers: {
            "X-User-Id": user.userId,
            "X-User-Role": user.role,
            "X-User-Email": user.email,
            ...(user.agentId && { "X-Agent-Id": user.agentId }),
          },
        });

        if (toolsResponse.ok) {
          const data = await toolsResponse.json();
          setTools(data.tools || []);
          setBackendConnected(true);
        }
      } catch (error) {
        console.error("Failed to fetch data:", error);
        setTools([]);
        setBackendConnected(false);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user.id, user.role, user.email, user.agentId]);

  const headers = {
    "X-User-Id": user.userId,
    "X-User-Role": user.role,
    "X-User-Email": user.email,
    ...(user.agentId && { "X-Agent-Id": user.agentId }),
  };

  return (
    <CopilotKit
      runtimeUrl="/api/copilotkit"
      headers={headers}
      agent="ticketflow-agent"
    >
      {loading ? (
        <div className="flex items-center justify-center h-64">
          <p className="text-muted-foreground">Ladowanie narzedzi...</p>
        </div>
      ) : (
        <ChatWithUserContext
          user={user}
          setDemoUser={setDemoUser}
          tools={tools}
          backendConnected={backendConnected}
          loading={loading}
        />
      )}
    </CopilotKit>
  );
}

function getInitialMessage(role: UserRole): string {
  switch (role) {
    case "client":
      return "Czesc! Jestem asystentem TicketFlow. Moge pomoc Ci sprawdzic status Twojego zgloszenia lub eskalowac pilne sprawy. Podaj numer zgloszenia lub opisz swoj problem.";
    case "agent":
      return "Czesc! Jestem asystentem TicketFlow. Moge pomoc Ci zarzadzac Twoimi ticketami - wyswietlac liste, sprawdzac szczegoly, dodawac notatki i rozwiazywac problemy.";
    case "supervisor":
      return "Czesc! Jestem asystentem TicketFlow. Jako supervisor masz pelny dostep do wszystkich ticketow i mozesz przypisywac agentow. Jak moge Ci pomoc?";
    case "admin":
      return "Czesc! Jestem asystentem TicketFlow. Jako admin masz pelny dostep do systemu. Jak moge Ci pomoc?";
  }
}
