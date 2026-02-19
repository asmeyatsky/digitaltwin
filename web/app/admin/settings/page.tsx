"use client";

import DashboardLayout from "@/components/DashboardLayout";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export default function SettingsPage() {
  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h2 className="text-2xl font-bold text-warmgray-900">
            System Settings
          </h2>
          <p className="text-warmgray-500 mt-1">
            System configuration and external service links
          </p>
        </div>

        {/* System info */}
        <div className="card">
          <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
            System Information
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <span className="text-xs text-warmgray-500">API URL</span>
              <p className="text-sm font-mono text-warmgray-800 mt-0.5">
                {API_URL}
              </p>
            </div>
            <div>
              <span className="text-xs text-warmgray-500">Dashboard Version</span>
              <p className="text-sm font-mono text-warmgray-800 mt-0.5">
                1.0.0
              </p>
            </div>
            <div>
              <span className="text-xs text-warmgray-500">Environment</span>
              <p className="text-sm font-mono text-warmgray-800 mt-0.5">
                {process.env.NODE_ENV}
              </p>
            </div>
            <div>
              <span className="text-xs text-warmgray-500">Next.js</span>
              <p className="text-sm font-mono text-warmgray-800 mt-0.5">
                14.2.15
              </p>
            </div>
          </div>
        </div>

        {/* External links */}
        <div className="card">
          <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
            External Services
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <a
              href="http://localhost:3000"
              target="_blank"
              rel="noopener noreferrer"
              className="border border-warmgray-200 rounded-lg p-4 hover:border-primary-300 hover:shadow-sm transition-all group"
            >
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-orange-100 flex items-center justify-center">
                  <svg className="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                  </svg>
                </div>
                <div>
                  <p className="text-sm font-medium text-warmgray-900 group-hover:text-primary-600 transition-colors">
                    Grafana Dashboard
                  </p>
                  <p className="text-xs text-warmgray-500">
                    Metrics & monitoring
                  </p>
                </div>
              </div>
            </a>

            <a
              href="http://localhost:16686"
              target="_blank"
              rel="noopener noreferrer"
              className="border border-warmgray-200 rounded-lg p-4 hover:border-primary-300 hover:shadow-sm transition-all group"
            >
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center">
                  <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </div>
                <div>
                  <p className="text-sm font-medium text-warmgray-900 group-hover:text-primary-600 transition-colors">
                    Jaeger Tracing
                  </p>
                  <p className="text-xs text-warmgray-500">
                    Distributed tracing
                  </p>
                </div>
              </div>
            </a>

            <a
              href="http://localhost:9090"
              target="_blank"
              rel="noopener noreferrer"
              className="border border-warmgray-200 rounded-lg p-4 hover:border-primary-300 hover:shadow-sm transition-all group"
            >
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-red-100 flex items-center justify-center">
                  <svg className="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                  </svg>
                </div>
                <div>
                  <p className="text-sm font-medium text-warmgray-900 group-hover:text-primary-600 transition-colors">
                    Prometheus
                  </p>
                  <p className="text-xs text-warmgray-500">
                    Metrics storage & queries
                  </p>
                </div>
              </div>
            </a>
          </div>
        </div>

        {/* API Endpoints reference */}
        <div className="card">
          <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
            API Endpoints
          </h3>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-warmgray-200">
                  <th className="table-header">Service</th>
                  <th className="table-header">Base Path</th>
                  <th className="table-header">Description</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-warmgray-100">
                {[
                  { service: "Auth", path: "/api/auth", desc: "Authentication & user management" },
                  { service: "Conversations", path: "/api/conversation", desc: "Chat sessions & history" },
                  { service: "Insights", path: "/api/insights", desc: "Emotional analytics" },
                  { service: "Community", path: "/api/community", desc: "Forums & peer support" },
                  { service: "Moderation", path: "/api/moderation", desc: "Content moderation" },
                  { service: "Learning", path: "/api/learning", desc: "Educational paths" },
                  { service: "Therapy", path: "/api/therapy", desc: "Therapist marketplace" },
                  { service: "Achievements", path: "/api/achievements", desc: "Gamification system" },
                  { service: "Voice", path: "/api/voice", desc: "TTS, STT, voice cloning" },
                  { service: "Avatar", path: "/api/avatar", desc: "3D avatar generation" },
                ].map((ep) => (
                  <tr key={ep.service}>
                    <td className="table-cell font-medium">{ep.service}</td>
                    <td className="table-cell font-mono text-xs text-primary-600">
                      {ep.path}
                    </td>
                    <td className="table-cell text-warmgray-500">{ep.desc}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
