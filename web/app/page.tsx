"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import DashboardLayout from "@/components/DashboardLayout";
import { getModerationStats, getLearningPaths, ModerationStats } from "@/lib/api";

interface DashboardStats {
  totalUsers: number;
  activeConversations: number;
  pendingReports: number;
  learningPaths: number;
}

export default function DashboardHome() {
  const [stats, setStats] = useState<DashboardStats>({
    totalUsers: 0,
    activeConversations: 0,
    pendingReports: 0,
    learningPaths: 0,
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadStats() {
      try {
        const [modRes, learnRes] = await Promise.allSettled([
          getModerationStats(),
          getLearningPaths(),
        ]);

        const modStats =
          modRes.status === "fulfilled" ? modRes.value.data : null;
        const learnData =
          learnRes.status === "fulfilled" ? learnRes.value.data : null;

        setStats({
          totalUsers: 128, // placeholder — no user-count endpoint exposed
          activeConversations: 42, // placeholder
          pendingReports: modStats?.pendingCount ?? 0,
          learningPaths: learnData?.paths?.length ?? 0,
        });
      } catch {
        // keep defaults
      } finally {
        setLoading(false);
      }
    }

    loadStats();
  }, []);

  const statCards = [
    {
      label: "Total Users",
      value: stats.totalUsers,
      color: "text-primary-600",
      bg: "bg-primary-50",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
        </svg>
      ),
    },
    {
      label: "Active Conversations",
      value: stats.activeConversations,
      color: "text-green-600",
      bg: "bg-green-50",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
        </svg>
      ),
    },
    {
      label: "Pending Reports",
      value: stats.pendingReports,
      color: "text-amber-600",
      bg: "bg-amber-50",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
        </svg>
      ),
    },
    {
      label: "Learning Paths",
      value: stats.learningPaths,
      color: "text-blue-600",
      bg: "bg-blue-50",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
        </svg>
      ),
    },
  ];

  const quickLinks = [
    { label: "Review Moderation Queue", href: "/admin/moderation", description: "Review pending content reports" },
    { label: "View Emotion Insights", href: "/insights", description: "Analyze emotional analytics and trends" },
    { label: "Manage Community", href: "/community", description: "Browse community groups and posts" },
    { label: "Verify Therapists", href: "/admin/therapists", description: "Review therapist credentials" },
    { label: "Learning Paths", href: "/learning", description: "View and manage educational content" },
    { label: "System Settings", href: "/admin/settings", description: "Configure system preferences" },
  ];

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Page header */}
        <div>
          <h2 className="text-2xl font-bold text-warmgray-900">Dashboard</h2>
          <p className="text-warmgray-500 mt-1">
            Overview of the Digital Twin platform
          </p>
        </div>

        {/* Stat cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {statCards.map((card) => (
            <div key={card.label} className="stat-card">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-warmgray-500">
                  {card.label}
                </span>
                <span className={`p-2 rounded-lg ${card.bg} ${card.color}`}>
                  {card.icon}
                </span>
              </div>
              <p className={`text-3xl font-bold mt-2 ${card.color}`}>
                {loading ? "--" : card.value}
              </p>
            </div>
          ))}
        </div>

        {/* Quick links */}
        <div>
          <h3 className="text-lg font-semibold text-warmgray-900 mb-4">
            Quick Actions
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {quickLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="card hover:shadow-md hover:border-primary-200 transition-all duration-150 group"
              >
                <h4 className="text-sm font-semibold text-warmgray-900 group-hover:text-primary-600 transition-colors">
                  {link.label}
                </h4>
                <p className="text-xs text-warmgray-500 mt-1">
                  {link.description}
                </p>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
