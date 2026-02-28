"use client"

import { Providers } from "./providers";
import { CustomNavigationMenu } from '@/components/custom/custom-nav';
import { Footer } from "@/components/custom/footer";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from "@/components/ui/card";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faTicket } from "@fortawesome/free-solid-svg-icons";
import { Toaster } from "sonner"
import { AgentRequired } from "@/components/custom/agent-required"

export function LayoutContent({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <Providers>
      <div className="mx-auto w-full h-full flex flex-col bg-background border-border/40 dark:border-border min-[1800px]:max-w-[1536px] min-[1800px]:border-x">
        <header className="border-b">
          <div className="container flex h-14 items-center px-8 w-full">
            <CustomNavigationMenu />
          </div>
        </header>

        <div className="container flex-1 py-8 px-8 w-full mx-auto">
          <main className="flex flex-col gap-8 items-center sm:items-start w-full h-full mx-auto">
            <Card className="w-full h-full mx-auto border-none">
              <CardHeader>
                <CardTitle>
                  <h3 className="scroll-m-20 pb-2 text-3xl font-semibold tracking-tight first:mt-0">
                    <FontAwesomeIcon className="pr-4" icon={faTicket} /> 
                    Tickety
                  </h3>
                </CardTitle>
                <CardDescription>
                  Tu nadzorcy analizują zgłoszenia a agenci service desk rozwiązują problemy
                </CardDescription>
              </CardHeader>
              <CardContent>
                <AgentRequired>
                  {children}
                </AgentRequired>
              </CardContent>
            </Card>
          </main>
        </div>
        <div className="w-full mx-auto">
          <Footer />
        </div>
      </div>
      <Toaster />
    </Providers>
  )
} 