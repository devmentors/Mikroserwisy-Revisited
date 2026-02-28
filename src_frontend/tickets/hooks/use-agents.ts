import { useQuery } from "@tanstack/react-query"
import { API } from '@/lib/api-config';

interface Agent {
  id: string
  userId: string
  fullName: string
  position: 'Supervisor' | 'Agent'
  avatarUrl: string
}

async function getAgents(): Promise<Agent[]> {
  const response = await fetch(`${API.tickets}/agents`)
  if (!response.ok) {
    throw new Error('Nie udało się pobrać agentów')
  }
  return response.json()
}

export function useAgents() {
  return useQuery({
    queryKey: ['agents'],
    queryFn: getAgents,
  })
}

export function useAgent(id: string) {
  return useQuery({
    queryKey: ['agents', id],
    queryFn: async () => {
      const response = await fetch(`${API.tickets}/agents/${id}`)
      if (!response.ok) {
        throw new Error('Nie udało się pobrać agenta')
      }
      return response.json()
    },
  })
}

export type { Agent } 