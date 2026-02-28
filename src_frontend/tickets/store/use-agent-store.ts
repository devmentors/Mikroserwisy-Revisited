import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { Agent } from '@/hooks/use-agents'

interface AgentState {
  selectedAgent: Agent | null
  setSelectedAgent: (agent: Agent | null) => void
}

export const useAgentStore = create<AgentState>()(
  persist(
    (set) => ({
      selectedAgent: null,
      setSelectedAgent: (agent) => set({ selectedAgent: agent }),
    }),
    {
      name: 'agent-storage',
      storage: createJSONStorage(() => localStorage),
    }
  )
) 