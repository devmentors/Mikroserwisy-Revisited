"use client"

import { faCheckCircle, faSpinner, faPlusCircle, faCircle, faCircleExclamation, faTriangleExclamation, faFire, faRotateRight } from "@fortawesome/free-solid-svg-icons"
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { ColumnDef, Row } from "@tanstack/react-table"
import { MoreHorizontal } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

import { InquiryCategory, InquiryStatus } from '@/app/types/enums'
import { inquiryCategoryTranslations, inquiryStatusTranslations } from "@/app/lib/translations";
import { Inquiry } from "@/app/types/inquiry";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { useState, useEffect } from "react"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { DialogFooter } from "@/components/ui/dialog"
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import * as z from "zod"
import { Separator } from "@/components/ui/separator"
import { toast } from "sonner"
import { getTicketComments, addTicketComment } from "@/app/services/ticketService"

type InquiryWithDialogState = Inquiry & {
  showDetails?: boolean;
};

const commentFormSchema = z.object({
  comment: z.string().min(1, "Komentarz nie może być pusty"),
})

export const columns = (
  onRefresh: () => void
): ColumnDef<InquiryWithDialogState>[] => [
  {
    accessorKey: "title",
    header: "Tytuł",
    cell: ({ row }) => {
      const title = row.getValue("title") as string;
      const category = row.original.category;
     
      const categoryLabel = inquiryCategoryTranslations[category?.toLowerCase() as InquiryCategory];
      
      return (
        <div className="flex items-center">
          <span className="font-medium text-muted-foreground mr-2">[{categoryLabel || category}]</span>
          <span>{title}</span>
        </div>
      );
    }
  },
  {
    accessorKey: "status",
    header: "Status",
    cell: ({ row }) => {
      const status = row.getValue("status") as InquiryStatus;
      
      const statusConfig = {
        [InquiryStatus.NEW]: { 
          label: inquiryStatusTranslations[InquiryStatus.NEW], 
          variant: "outline",
          icon: faPlusCircle
        },
        [InquiryStatus.IN_PROGRESS]: { 
          label: inquiryStatusTranslations[InquiryStatus.IN_PROGRESS], 
          variant: "outline",
          icon: faSpinner
        },
        [InquiryStatus.RESOLVED]: { 
          label: inquiryStatusTranslations[InquiryStatus.RESOLVED], 
          variant: "secondary",
          icon: faCheckCircle
        },
        [InquiryStatus.CLOSED]: { 
          label: inquiryStatusTranslations[InquiryStatus.CLOSED], 
          variant: "secondary",
          icon: faCheckCircle
        },
      } as const;

      const config = statusConfig[status] || statusConfig[InquiryStatus.NEW];

      return (
        <div className="flex justify-start items-center">
          <Badge variant={config.variant}>
            <FontAwesomeIcon icon={config.icon} className="mr-2" />
            {config.label}
          </Badge>
        </div>
      )
    }
  },
  {
    accessorKey: "createdAt",
    header: "Data utworzenia",
    cell: ({ row }) => {
      const date = new Date(row.getValue("createdAt"));
      return date.toLocaleString('pl-PL', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    }
  },
  {
    id: "actions",
    cell: ({ row }) => {
      const inquiry = row.original
      const [showCommentForm, setShowCommentForm] = useState(false)
 
      return (
        <>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">
                <span className="sr-only">Otwórz menu</span>
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Akcje</DropdownMenuLabel>
              <DropdownMenuItem
                onClick={() => navigator.clipboard.writeText(inquiry.id.toString())}
              >
                Skopiuj ID zgłoszenia
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => setShowCommentForm(true)}>
                Zobacz szczegóły
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          
          <AddCommentDialog 
            inquiry={inquiry}
            open={showCommentForm}
            onOpenChange={setShowCommentForm}
            onCommentAdded={onRefresh}
          />
        </>
      )
    },
  },
]

function AddCommentDialog({ 
  inquiry, 
  open, 
  onOpenChange,
  onCommentAdded
}: { 
  inquiry: Inquiry; 
  open: boolean; 
  onOpenChange: (open: boolean) => void;
  onCommentAdded?: () => void;
}) {
  const form = useForm<z.infer<typeof commentFormSchema>>({
    resolver: zodResolver(commentFormSchema),
    defaultValues: {
      comment: "",
    },
  })

  const [ticketComments, setTicketComments] = useState<string>("");
  const comments = ticketComments?.split('\n').filter(comment => comment.trim()) || [];

  // Add useEffect to fetch ticket comments when dialog opens
  useEffect(() => {
    async function fetchTicketComments() {
      if (inquiry.ticketId && open) {
        try {
          const comments = await getTicketComments(inquiry.ticketId);
          setTicketComments(comments);
        } catch (error) {
          console.error('Failed to fetch ticket comments:', error);
          toast.error("Nie udało się pobrać komentarzy");
        }
      }
    }

    fetchTicketComments();
  }, [inquiry.ticketId, open]);

  async function onSubmit(values: z.infer<typeof commentFormSchema>) {
    try {
      if (!inquiry.ticketId) return;
      
      await addTicketComment(inquiry.ticketId, values.comment);
      const updatedComments = await getTicketComments(inquiry.ticketId);
      setTicketComments(updatedComments);
      toast.success("Komentarz został dodany");
      form.reset();
    } catch (error) {
      toast.error("Nie udało się dodać komentarza");
      console.error(error);
    }
  }

  // Add useEffect for cleanup
  useEffect(() => {
    if (!open) {
      document.body.style.pointerEvents = '';
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.pointerEvents = '';
      document.body.style.overflow = '';
    };
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="text-2xl font-bold">{inquiry.title}</DialogTitle>
        </DialogHeader>
        <Separator className="my-4" />
        
        {/* Basic inquiry info */}
        <div className="grid gap-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <h3 className="font-semibold mb-2">Kategoria</h3>
              <p className="text-muted-foreground">
                {inquiryCategoryTranslations[inquiry.category?.toLowerCase() as InquiryCategory] || inquiry.category}
              </p>
            </div>
            <div>
              <h3 className="font-semibold mb-2">Status</h3>
              <p className="text-muted-foreground">
                {inquiryStatusTranslations[inquiry.status]}
              </p>
            </div>
            <div>
              <h3 className="font-semibold mb-2">Data utworzenia</h3>
              <p className="text-muted-foreground">
                {new Date(inquiry.createdAt).toLocaleString('pl-PL')}
              </p>
            </div>
          </div>
          
          {inquiry.description && (
            <div>
              <h3 className="font-semibold mb-2">Opis</h3>
              <p className="text-muted-foreground whitespace-pre-wrap">{inquiry.description}</p>
            </div>
          )}
        </div>

        <Separator className="my-4" />
        
        {/* Comments and ticket section */}
        <div className="space-y-4">
          <div>
            <h3 className="font-semibold mb-2">Powiązany ticket</h3>
            <p className="text-muted-foreground">
              {inquiry.ticketId ? inquiry.ticketId : 'Brak powiązanego ticketu'}
            </p>
          </div>
          
          <div>
            <h3 className="font-semibold mb-2">Komentarze</h3>
            {inquiry.ticketId ? (
              comments.length > 0 ? (
                <div className="space-y-2">
                  {comments.map((comment, index) => (
                    <div key={index} className="p-2 bg-muted rounded-md">
                      <p className="text-sm text-muted-foreground">{comment}</p>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Brak komentarzy</p>
              )
            ) : (
              <p className="text-sm text-muted-foreground">Brak powiązanego ticketu</p>
            )}
          </div>
          
          {inquiry.ticketId ? (
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="comment"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Nowy komentarz</FormLabel>
                      <FormControl>
                        <Textarea 
                          placeholder="Wpisz swój komentarz..." 
                          {...field} 
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
                <DialogFooter>
                  <Button type="submit">Dodaj komentarz</Button>
                </DialogFooter>
              </form>
            </Form>
          ) : (
            <div className="rounded-md bg-muted p-4">
              <p className="text-sm text-muted-foreground">
                Aby dodać komentarz, zgłoszenie musi być powiązane z ticketem.
              </p>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

export function DataTableToolbar({ onRefresh }: { onRefresh: () => void }) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex flex-1 items-center space-x-2">
        {/* Your existing toolbar content */}
      </div>
      <div className="flex items-center space-x-2">
        <Button
          variant="outline"
          size="sm"
          onClick={onRefresh}
          className="h-8"
        >
          <FontAwesomeIcon icon={faRotateRight} className="mr-2 h-4 w-4" />
          Odśwież
        </Button>
        {/* Your existing "Dodaj zgłoszenie" button */}
      </div>
    </div>
  )
}