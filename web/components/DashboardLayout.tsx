"use client";

import Sidebar from "./Sidebar";
import { useAuth } from "@/lib/auth";

interface DashboardLayoutProps {
  children: React.ReactNode;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  const { checked, user, logout } = useAuth();

  if (!checked) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-warmgray-100">
        <div className="text-warmgray-500 text-sm">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-warmgray-100">
      <Sidebar />

      {/* Main content */}
      <div className="lg:ml-64">
        {/* Top bar */}
        <header className="sticky top-0 z-20 bg-white border-b border-warmgray-200 px-6 py-3">
          <div className="flex items-center justify-between">
            <h1 className="text-lg font-semibold text-warmgray-900 ml-10 lg:ml-0">
              Digital Twin Admin
            </h1>
            <div className="flex items-center gap-4">
              <div className="text-right">
                <p className="text-sm font-medium text-warmgray-900">
                  {user?.username ?? "Admin"}
                </p>
                <p className="text-xs text-warmgray-500">
                  {user?.roles?.join(", ") ?? "user"}
                </p>
              </div>
              <button
                onClick={logout}
                className="text-sm text-warmgray-500 hover:text-warmgray-700 transition-colors"
                title="Sign out"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
              </button>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="p-6">{children}</main>
      </div>
    </div>
  );
}
