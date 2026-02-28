import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { Client, CLIENTS } from '@/lib/clients'

interface ClientState {
  selectedClient: Client | null
  setSelectedClient: (client: Client | null) => void
}

export const useClientStore = create<ClientState>()(
  persist(
    (set) => ({
      selectedClient: null,
      setSelectedClient: (client) => set({ selectedClient: client }),
    }),
    {
      name: 'client-storage',
      storage: createJSONStorage(() => localStorage),
      // Migrate old clients without userId
      onRehydrateStorage: () => (state) => {
        if (state?.selectedClient && !state.selectedClient.userId) {
          const updated = CLIENTS.find(c => c.personToken === state.selectedClient?.personToken);
          if (updated) {
            state.setSelectedClient(updated);
          }
        }
      },
    }
  )
)
