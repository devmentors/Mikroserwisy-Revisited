"use client"

import { useClientStore } from "@/store/use-client-store"
import { ClientSelect } from "./client-select"
import { Card } from "@/components/ui/card"
import { CLIENTS } from "@/lib/clients"

interface ClientRequiredProps {
  children: React.ReactNode
}

export function ClientRequired({ children }: ClientRequiredProps) {
  const { selectedClient, setSelectedClient } = useClientStore()

  if (!selectedClient) {
    return (
      <div className="min-h-[calc(100vh-4rem)] flex items-start justify-center pt-8">
        <Card className="p-8 max-w-md w-full space-y-4">
          <h2 className="text-xl font-semibold text-center">Wybierz klienta</h2>
          <p className="text-muted-foreground text-center">
            Aby kontynuować, musisz wybrać klienta, w kontekście którego będziesz przeglądać zgłoszenia.
          </p>
          <div className="flex justify-center">
            <ClientSelect
              clients={CLIENTS}
              value={selectedClient?.personToken}
              onChange={(personToken) => {
                const client = CLIENTS.find((c) => c.personToken === personToken)
                setSelectedClient(client || null)
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
