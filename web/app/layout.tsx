import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import { Suspense } from "react";
import { UserMenu } from "@/components/user-menu";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "SkillLink",
  description: "Connect with people through the skills you share.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <header className="border-b border-black/10 dark:border-white/10">
          <nav className="mx-auto flex w-full max-w-2xl items-center gap-5 px-4 py-3">
            <Link href="/" className="font-bold">
              SkillLink
            </Link>
            <Link href="/people" className="text-sm">
              Find people
            </Link>
            <Link href="/connections" className="text-sm">
              Connections
            </Link>
            <div className="ml-auto">
              {/* The session read is pushed down into this boundary, so the layout itself stays static. */}
              <Suspense fallback={null}>
                <UserMenu />
              </Suspense>
            </div>
          </nav>
        </header>
        {children}
      </body>
    </html>
  );
}
