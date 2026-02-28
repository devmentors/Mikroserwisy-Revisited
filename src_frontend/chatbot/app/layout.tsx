import { Inter } from "next/font/google";
import "./globals.css";
import "@copilotkit/react-ui/styles.css";
import { Metadata } from "next";
import { CustomNavigationMenu } from "@/components/custom/custom-nav";
import { Footer } from "@/components/custom/footer";
import { config } from "@fortawesome/fontawesome-svg-core";
import "@fortawesome/fontawesome-svg-core/styles.css";

const inter = Inter({
  subsets: ["latin", "latin-ext"],
  display: "swap",
  variable: "--font-inter",
});

config.autoAddCss = false;

export const metadata: Metadata = {
  title: "TicketFlow - Chat",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pl" className={`h-full dark ${inter.variable}`}>
      <body className={`${inter.className} h-full bg-background antialiased`}>
        <div className="mx-auto w-full h-full flex flex-col bg-background border-border/40 dark:border-border min-[1800px]:max-w-[1536px] min-[1800px]:border-x">
          <header className="border-b">
            <div className="container flex h-14 items-center px-8">
              <CustomNavigationMenu />
            </div>
          </header>
          <div className="container flex-1 py-8 px-8 w-full mx-auto overflow-hidden">
            <main className="flex flex-col gap-8 items-center sm:items-start w-full h-full mx-auto">
              {children}
            </main>
          </div>
          <div className="w-full mx-auto">
            <Footer />
          </div>
        </div>
      </body>
    </html>
  );
}
