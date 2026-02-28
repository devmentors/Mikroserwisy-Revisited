"use client"

import { useAgentStore } from "@/store/use-agent-store"
import { useAgents } from "@/hooks/use-agents"
import { AgentSelect } from "./agent-select"
import { Card } from "@/components/ui/card"

interface AgentRequiredProps {
  children: React.ReactNode
}

export function AgentRequired({ children }: AgentRequiredProps) {
  const { selectedAgent, setSelectedAgent } = useAgentStore()
  const { data: agents, isLoading } = useAgents()

  if (isLoading) {
    return <div>Ładowanie...</div>
  }

  if (!selectedAgent) {
    return (
      <div className="min-h-[calc(100vh-4rem)] flex items-start justify-center pt-8">
        <Card className="p-8 max-w-md w-full space-y-4">
          <h2 className="text-xl font-semibold text-center">Wybierz agenta</h2>
          <p className="text-muted-foreground text-center">
            Aby kontynuować, musisz wybrać agenta, w kontekście którego będziesz pracować.
          </p>
          <div className="flex justify-center">
            <AgentSelect
              agents={agents || []}
              value={selectedAgent?.id}
              onChange={(id) => {
                const agent = agents?.find((a) => a.id === id)
                setSelectedAgent(agent || null)
              }}
              size="large"
            />
          </div>
        </Card>
      </div>
    )
  }

  return <>{children}</>
} 