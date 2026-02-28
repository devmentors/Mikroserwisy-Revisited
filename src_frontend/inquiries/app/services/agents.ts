import { API } from '@/lib/api-config';

export async function getAgent(id: string): Promise<Agent> {
  const response = await fetch(`${API.tickets}/agents/${id}`)
  if (!response.ok) {
    throw new Error('Nie udało się pobrać agenta')
  }
  return response.json()
} 