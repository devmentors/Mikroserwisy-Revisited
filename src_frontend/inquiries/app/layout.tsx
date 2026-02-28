import { GeistSans } from 'geist/font/sans';
import "./globals.css";
import { Providers } from "./providers";
import { config } from '@fortawesome/fontawesome-svg-core';
import '@fortawesome/fontawesome-svg-core/styles.css';
import { Metadata } from 'next';
import { ClientLayout } from '@/app/components/client-layout';

config.autoAddCss = false;

export const metadata: Metadata = {
  title: "TicketFlow - Zgłoszenia",
}; 

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="h-full">
      <body className={`${GeistSans.className} h-full dark:bg-background`}>
        <Providers>
          <ClientLayout>{children}</ClientLayout>
        </Providers>
      </body>
    </html>
  );
}
