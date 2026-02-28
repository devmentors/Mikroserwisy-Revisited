import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { DemoUser, DEMO_USERS } from '@/components/custom/user-selector'

interface UserState {
  selectedUser: DemoUser
  setSelectedUser: (user: DemoUser) => void
  _hasHydrated: boolean
  setHasHydrated: (state: boolean) => void
}

// Get first client as default
const defaultUser = DEMO_USERS.find(u => u.role === 'client') || DEMO_USERS[0]

export const useUserStore = create<UserState>()(
  persist(
    (set) => ({
      selectedUser: defaultUser,
      setSelectedUser: (user) => set({ selectedUser: user }),
      _hasHydrated: false,
      setHasHydrated: (state) => set({ _hasHydrated: state }),
    }),
    {
      name: 'inquiries-user-storage',
      storage: createJSONStorage(() => localStorage),
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true)
      },
    }
  )
)
