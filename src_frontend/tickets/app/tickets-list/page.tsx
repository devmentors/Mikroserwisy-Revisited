"use client"

import React from "react"
import { DataTable } from "@/components/custom/data-table"
import { columns } from "./ticket"
import { useTickets } from "@/hooks/use-tickets"
import { Button } from "@/components/ui/button"
import { RotateCw, Loader2 } from "lucide-react"

export default function TicketsList() {
  const [page, setPage] = React.useState(1);
  const limit = 10;
  
  const { data, isLoading, isFetching, refetch } = useTickets({
    page,
    limit
  });
  const totalPages = data ? Math.ceil(data.totalCount / limit) : 1;

  return (
    <div className="container mx-auto py-4">
      <div className="mb-4 flex justify-between items-center">
        <h1 className="text-2xl font-bold">Lista ticketów</h1>
        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isFetching}
        >
          <RotateCw className={`h-4 w-4 mr-2 ${isFetching ? 'animate-spin' : ''}`} />
          Odśwież
        </Button>
      </div>
      {isFetching ? (
        <div className="flex justify-center items-center h-[400px]">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <DataTable 
          columns={columns} 
          data={data?.data || []}
          pagination={{
            page,
            pageSize: limit,
            totalPages,
            onPageChange: setPage
          }}
        />
      )}
    </div>
  )
}
