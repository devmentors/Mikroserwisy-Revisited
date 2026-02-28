"use client"

import React, { useState, useEffect, useRef, useCallback } from "react"
import * as inquiryService from "@/app/services/inquiryService";
import { columns, DataTableToolbar } from "./inquiry";
import { Inquiry } from "@/app/types/inquiry";
import { DataTable } from "@/components/custom/data-table";
import { Button } from "@/components/ui/button";
import { useRouter } from "next/navigation";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { faRotateRight, faPlus } from "@fortawesome/free-solid-svg-icons"

export default function InquiriesPage() {
  const router = useRouter();
  const [data, setData] = useState<Inquiry[]>([]);
  const [pageCount, setPageCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const loadingTimeoutRef = useRef<NodeJS.Timeout>();
  
  const fetchData = useCallback(async (pageIndex: number, pageSize: number) => {
    if (loadingTimeoutRef.current) {
      clearTimeout(loadingTimeoutRef.current);
    }

    loadingTimeoutRef.current = setTimeout(() => {
      setIsLoading(true);
    }, 500);

    try {
      const result = await inquiryService.getPaginatedInquiries(pageIndex, pageSize);
      setData(result.data);
      setPageCount(Math.ceil(result.total / pageSize));
    } catch (error) {
      console.error('Error fetching data:', error);
    } finally {
      if (loadingTimeoutRef.current) {
        clearTimeout(loadingTimeoutRef.current);
      }
      setIsLoading(false);
    }
  }, []);

  const handlePaginationChange = useCallback(({ pageIndex, pageSize }: { pageIndex: number; pageSize: number }) => {
    fetchData(pageIndex, pageSize);
  }, [fetchData]);

  useEffect(() => {
    fetchData(0, 10);
    return () => {
      if (loadingTimeoutRef.current) {
        clearTimeout(loadingTimeoutRef.current);
      }
    };
  }, [fetchData]);

  const handleRefresh = useCallback(() => {
    fetchData(0, 10);
  }, [fetchData]);

  return (
    <div className="container mx-auto py-4">
      <div className="flex justify-between items-end mb-6 w-full" style={{ marginTop: '-76px' }}>
        <div>&nbsp;</div>
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            onClick={handleRefresh}
          >
            <FontAwesomeIcon icon={faRotateRight} className="mr-2 h-4 w-4" />
            Odśwież
          </Button>
          <Button 
            onClick={() => router.push('/submit-inquiry')}
          >
            <FontAwesomeIcon icon={faPlus} className="mr-2 h-4 w-4" />
            Dodaj zgłoszenie
          </Button>
        </div>
      </div>
      <DataTable<Inquiry, Inquiry>
        columns={columns(handleRefresh)}
        data={data}
        pageCount={pageCount}
        isLoading={isLoading}
        onPaginationChange={handlePaginationChange}
      />
    </div>
  );
}
