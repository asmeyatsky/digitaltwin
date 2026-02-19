"use client";

import { useEffect, useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { getEmotionInsights, EmotionInsights } from "@/lib/api";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
} from "recharts";

type Period = "week" | "month" | "all";

export default function InsightsPage() {
  const [period, setPeriod] = useState<Period>("week");
  const [insights, setInsights] = useState<EmotionInsights | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const res = await getEmotionInsights(period);
        setInsights(res.data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load insights");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [period]);

  const distributionData = insights
    ? Object.entries(insights.emotionDistribution).map(([emotion, count]) => ({
        emotion,
        count,
      }))
    : [];

  const timelineData = insights?.moodTimeline ?? [];

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold text-warmgray-900">
              Emotional Insights
            </h2>
            <p className="text-warmgray-500 mt-1">
              Analytics on emotional patterns and trends
            </p>
          </div>
          <div className="flex gap-2">
            {(["week", "month", "all"] as Period[]).map((p) => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                className={`px-4 py-2 text-sm rounded-lg font-medium transition-colors ${
                  period === p
                    ? "bg-primary-500 text-white"
                    : "bg-white text-warmgray-600 border border-warmgray-300 hover:bg-warmgray-50"
                }`}
              >
                {p === "all" ? "All Time" : p.charAt(0).toUpperCase() + p.slice(1)}
              </button>
            ))}
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12 text-warmgray-500">
            Loading insights...
          </div>
        ) : insights ? (
          <>
            {/* Summary cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="stat-card">
                <span className="text-sm text-warmgray-500">Sessions</span>
                <p className="text-2xl font-bold text-primary-600">
                  {insights.sessionCount}
                </p>
              </div>
              <div className="stat-card">
                <span className="text-sm text-warmgray-500">
                  Avg Duration
                </span>
                <p className="text-2xl font-bold text-green-600">
                  {insights.averageDurationMinutes.toFixed(1)} min
                </p>
              </div>
              <div className="stat-card">
                <span className="text-sm text-warmgray-500">
                  Top Emotion
                </span>
                <p className="text-2xl font-bold text-amber-600">
                  {insights.topEmotions[0]?.emotion ?? "N/A"}
                </p>
              </div>
              <div className="stat-card">
                <span className="text-sm text-warmgray-500">
                  Unique Emotions
                </span>
                <p className="text-2xl font-bold text-blue-600">
                  {Object.keys(insights.emotionDistribution).length}
                </p>
              </div>
            </div>

            {/* Charts */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Emotion distribution */}
              <div className="card">
                <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
                  Emotion Distribution
                </h3>
                {distributionData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={distributionData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" />
                      <XAxis
                        dataKey="emotion"
                        tick={{ fontSize: 12, fill: "#78716c" }}
                        tickLine={false}
                      />
                      <YAxis
                        tick={{ fontSize: 12, fill: "#78716c" }}
                        tickLine={false}
                      />
                      <Tooltip
                        contentStyle={{
                          borderRadius: "8px",
                          border: "1px solid #e7e5e4",
                          fontSize: "13px",
                        }}
                      />
                      <Bar dataKey="count" fill="#6366f1" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <p className="text-sm text-warmgray-400 text-center py-12">
                    No data available
                  </p>
                )}
              </div>

              {/* Mood timeline */}
              <div className="card">
                <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
                  Mood Timeline
                </h3>
                {timelineData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={timelineData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" />
                      <XAxis
                        dataKey="date"
                        tick={{ fontSize: 12, fill: "#78716c" }}
                        tickLine={false}
                      />
                      <YAxis
                        domain={[-1, 1]}
                        tick={{ fontSize: 12, fill: "#78716c" }}
                        tickLine={false}
                      />
                      <Tooltip
                        contentStyle={{
                          borderRadius: "8px",
                          border: "1px solid #e7e5e4",
                          fontSize: "13px",
                        }}
                      />
                      <Line
                        type="monotone"
                        dataKey="valence"
                        stroke="#6366f1"
                        strokeWidth={2}
                        dot={{ fill: "#6366f1", r: 3 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                ) : (
                  <p className="text-sm text-warmgray-400 text-center py-12">
                    No data available
                  </p>
                )}
              </div>
            </div>

            {/* Top emotions table */}
            <div className="card">
              <h3 className="text-sm font-semibold text-warmgray-700 mb-4">
                Top Emotions
              </h3>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-warmgray-200">
                      <th className="table-header">Emotion</th>
                      <th className="table-header">Count</th>
                      <th className="table-header">Percentage</th>
                      <th className="table-header">Distribution</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-warmgray-100">
                    {insights.topEmotions.map((item) => (
                      <tr key={item.emotion}>
                        <td className="table-cell font-medium capitalize">
                          {item.emotion}
                        </td>
                        <td className="table-cell">{item.count}</td>
                        <td className="table-cell">
                          {item.percentage.toFixed(1)}%
                        </td>
                        <td className="table-cell">
                          <div className="w-full bg-warmgray-200 rounded-full h-2">
                            <div
                              className="bg-primary-500 h-2 rounded-full"
                              style={{ width: `${Math.min(item.percentage, 100)}%` }}
                            />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        ) : null}
      </div>
    </DashboardLayout>
  );
}
