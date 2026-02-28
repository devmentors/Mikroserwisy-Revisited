import { API } from '@/lib/api-config';

export async function getAgent(userId: string): Promise<Agent> {
  const response = await fetch(`${API.tickets}/users/${userId}`)
  if (!response.ok) {
    throw new Error('Nie udało się pobrać agenta')
  }
  return response.json()
} 