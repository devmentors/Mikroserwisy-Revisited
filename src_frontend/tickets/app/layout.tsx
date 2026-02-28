import type { Metadata } from "next";
import { GeistSans } from 'geist/font/sans';
import "./globals.css";
import { config } from '@fortawesome/fontawesome-svg-core'
import '@fortawesome/fontawesome-svg-core/styles.css'
import { LayoutContent } from "./layout-content";

config.autoAddCss = false

export const metadata: Metadata = {
  title: "TicketFlow - Tickety"
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className="h-full">
      <body className={`${GeistSans.className} h-full dark:bg-background`}>
        <LayoutContent>
          {children}
        </LayoutContent>
      </body>
    </html>
  )
}
