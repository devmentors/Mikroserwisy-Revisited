"use client"

import React, { useState } from 'react'
import { useForm, ControllerRenderProps, Control } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useRouter } from 'next/navigation'
import { toast } from "sonner"

import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Select, SelectTrigger, SelectValue, SelectItem, SelectContent } from '@/components/ui/select'
import { InquiryCategory, InquiryStatus } from '@/app/types/enums'
import { inquiryCategoryTranslations } from '@/app/lib/translations'
import { createInquiry } from '@/app/services/inquiryService';

const categoryOptions = [
  { value: InquiryCategory.TECHNICAL, label: inquiryCategoryTranslations[InquiryCategory.TECHNICAL] },
  { value: InquiryCategory.BILLING, label: inquiryCategoryTranslations[InquiryCategory.BILLING] },
  { value: InquiryCategory.OTHER, label: inquiryCategoryTranslations[InquiryCategory.OTHER] },
  { value: InquiryCategory.GENERAL, label: inquiryCategoryTranslations[InquiryCategory.GENERAL] },
] as const

const formSchema = z.object({
  title: z.string().min(3, "Tytuł musi mieć co najmniej 3 znaki"),
  name: z.string().min(2, "Imię musi mieć co najmniej 2 znaki"),
  email: z.string().email("Nieprawidłowy adres email"),
  description: z.string().min(10, "Opis musi mieć co najmniej 10 znaków"),
  category: z.nativeEnum(InquiryCategory),
})

type FormValues = z.infer<typeof formSchema>

const InquiryForm: React.FC = () => {
  const router = useRouter()
  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      title: '',
      name: '',
      email: '',
      description: '',
      category: InquiryCategory.TECHNICAL,
    }
  });

  const { handleSubmit } = form;

  const onSubmit = async (data: FormValues) => {
    try {
      await createInquiry({
        ...data
      });

      toast.success('Zgłoszenie zostało wysłane');
      router.push('/inquiries-list');
    } catch (error) {
      toast.error('Nie udało się wysłać zgłoszenia');
      console.error(error);
    }
  }

  return (
    <Card className="w-full max-w-2xl mx-auto">
      <CardHeader>
        <CardTitle>Nowe Zgłoszenie</CardTitle>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tytuł</FormLabel>
                  <FormControl>
                    <Input placeholder="Wprowadź tytuł zgłoszenia" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Imię</FormLabel>
                  <FormControl>
                    <Input placeholder="Wprowadź swoje imię" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email</FormLabel>
                  <FormControl>
                    <Input type="email" placeholder="twoj@email.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Opis</FormLabel>
                  <FormControl>
                    <Textarea 
                      placeholder="Opisz szczegółowo swoje zgłoszenie"
                      className="min-h-[100px]"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="category"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Kategoria</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Wybierz kategorię" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {categoryOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <Button type="submit" className="w-full">
              Wyślij Zgłoszenie
            </Button>
          </form>
        </Form>
      </CardContent>
    </Card>
  )
}

export default InquiryForm 