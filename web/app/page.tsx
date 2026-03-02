"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { isAuthenticated } from "@/lib/auth";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticated()) {
      router.replace("/chat");
    } else {
      router.replace("/login");
    }
  }, [router]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-companion-bg">
      <div className="text-warmgray-500 text-sm">Loading...</div>
    </div>
  );
}
