"use client";

import { useEffect, useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import {
  getLearningPaths,
  getLearningProgress,
  LearningPath,
  UserLearningProgress,
} from "@/lib/api";

const CATEGORY_LABELS: Record<string, string> = {
  EmotionalIntelligence: "Emotional Intelligence",
  Mindfulness: "Mindfulness",
  Communication: "Communication",
  StressManagement: "Stress Management",
  Resilience: "Resilience",
  SelfCare: "Self Care",
};

const CATEGORY_COLORS: Record<string, string> = {
  EmotionalIntelligence: "bg-purple-100 text-purple-700",
  Mindfulness: "bg-teal-100 text-teal-700",
  Communication: "bg-blue-100 text-blue-700",
  StressManagement: "bg-amber-100 text-amber-700",
  Resilience: "bg-green-100 text-green-700",
  SelfCare: "bg-pink-100 text-pink-700",
};

export default function LearningPage() {
  const [paths, setPaths] = useState<LearningPath[]>([]);
  const [progress, setProgress] = useState<UserLearningProgress[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const [pathsRes, progressRes] = await Promise.allSettled([
          getLearningPaths(),
          getLearningProgress(),
        ]);

        if (pathsRes.status === "fulfilled") {
          setPaths(pathsRes.value.data.paths);
        }
        if (progressRes.status === "fulfilled") {
          setProgress(progressRes.value.data.progress);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load learning data");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  function getProgressForPath(pathId: string): UserLearningProgress | undefined {
    return progress.find((p) => p.pathId === pathId);
  }

  function formatDuration(minutes: number): string {
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h2 className="text-2xl font-bold text-warmgray-900">
            Learning Paths
          </h2>
          <p className="text-warmgray-500 mt-1">
            Educational content and progress tracking
          </p>
        </div>

        {/* Summary stats */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">Total Paths</span>
            <p className="text-2xl font-bold text-primary-600">
              {loading ? "--" : paths.length}
            </p>
          </div>
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">In Progress</span>
            <p className="text-2xl font-bold text-amber-600">
              {loading
                ? "--"
                : progress.filter((p) => !p.completedAt).length}
            </p>
          </div>
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">Completed</span>
            <p className="text-2xl font-bold text-green-600">
              {loading
                ? "--"
                : progress.filter((p) => !!p.completedAt).length}
            </p>
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12 text-warmgray-500 text-sm">
            Loading learning paths...
          </div>
        ) : paths.length === 0 ? (
          <div className="text-center py-12 text-warmgray-400 text-sm">
            No learning paths available
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {paths.map((path) => {
              const prog = getProgressForPath(path.id);
              const completedModules = prog
                ? JSON.parse(prog.completedModules || "[]").length
                : 0;
              const percentComplete = path.moduleCount > 0
                ? Math.round((completedModules / path.moduleCount) * 100)
                : 0;

              return (
                <div key={path.id} className="card">
                  <div className="flex items-start justify-between mb-3">
                    <span
                      className={`badge text-[10px] ${
                        CATEGORY_COLORS[path.category] ?? "badge-gray"
                      }`}
                    >
                      {CATEGORY_LABELS[path.category] ?? path.category}
                    </span>
                    {prog?.completedAt && (
                      <span className="badge-green text-[10px]">Completed</span>
                    )}
                  </div>

                  <h3 className="text-sm font-semibold text-warmgray-900 mb-1">
                    {path.title}
                  </h3>
                  <p className="text-xs text-warmgray-500 line-clamp-2 mb-4">
                    {path.description}
                  </p>

                  <div className="flex items-center gap-4 text-xs text-warmgray-400 mb-3">
                    <span>{path.moduleCount} modules</span>
                    <span>{formatDuration(path.estimatedMinutes)}</span>
                  </div>

                  {prog && (
                    <div>
                      <div className="flex items-center justify-between text-xs mb-1">
                        <span className="text-warmgray-500">Progress</span>
                        <span className="font-medium text-warmgray-700">
                          {percentComplete}%
                        </span>
                      </div>
                      <div className="w-full bg-warmgray-200 rounded-full h-1.5">
                        <div
                          className="bg-primary-500 h-1.5 rounded-full transition-all"
                          style={{ width: `${percentComplete}%` }}
                        />
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}
