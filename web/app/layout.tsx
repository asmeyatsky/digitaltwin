import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Digital Twin Dashboard",
  description: "Admin and analytics dashboard for the Digital Twin emotional companion",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <link
          href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap"
          rel="stylesheet"
        />
      </head>
      <body className="font-sans bg-warmgray-100 text-warmgray-900 antialiased">
        {children}
      </body>
    </html>
  );
}
