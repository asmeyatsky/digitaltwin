"use client";

import { useEffect, useState, useCallback } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import {
  getPendingReports,
  reviewReport,
  dismissReport,
  getModerationStats,
  ContentReport,
  ModerationStats,
} from "@/lib/api";

const ACTION_OPTIONS = [
  { value: "Warning", label: "Warning" },
  { value: "ContentRemoved", label: "Content Removed" },
  { value: "UserSuspended", label: "User Suspended" },
  { value: "UserBanned", label: "User Banned" },
];

const STATUS_BADGE: Record<string, string> = {
  Pending: "badge-yellow",
  Reviewed: "badge-blue",
  Actioned: "badge-red",
  Dismissed: "badge-gray",
};

export default function ModerationPage() {
  const [reports, setReports] = useState<ContentReport[]>([]);
  const [stats, setStats] = useState<ModerationStats | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Review modal state
  const [modalReport, setModalReport] = useState<ContentReport | null>(null);
  const [modalAction, setModalAction] = useState("Warning");
  const [modalNotes, setModalNotes] = useState("");
  const [modalLoading, setModalLoading] = useState(false);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [reportsRes, statsRes] = await Promise.all([
        getPendingReports(page),
        getModerationStats(),
      ]);
      setReports(reportsRes.data.reports);
      setTotalCount(reportsRes.data.totalCount);
      setStats(statsRes.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load moderation data");
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  async function handleReview() {
    if (!modalReport) return;
    setModalLoading(true);
    try {
      await reviewReport(modalReport.id, modalAction, modalNotes || undefined);
      setModalReport(null);
      setModalAction("Warning");
      setModalNotes("");
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to review report");
    } finally {
      setModalLoading(false);
    }
  }

  async function handleDismiss(report: ContentReport) {
    try {
      await dismissReport(report.id, undefined);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to dismiss report");
    }
  }

  const totalPages = Math.ceil(totalCount / 20);

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h2 className="text-2xl font-bold text-warmgray-900">
            Content Moderation
          </h2>
          <p className="text-warmgray-500 mt-1">
            Review and action reported content
          </p>
        </div>

        {/* Stats bar */}
        {stats && (
          <div className="grid grid-cols-2 sm:grid-cols-5 gap-4">
            <div className="stat-card">
              <span className="text-xs text-warmgray-500">Total</span>
              <p className="text-xl font-bold text-warmgray-800">
                {stats.totalReports}
              </p>
            </div>
            <div className="stat-card">
              <span className="text-xs text-warmgray-500">Pending</span>
              <p className="text-xl font-bold text-yellow-600">
                {stats.pendingCount}
              </p>
            </div>
            <div className="stat-card">
              <span className="text-xs text-warmgray-500">Reviewed</span>
              <p className="text-xl font-bold text-blue-600">
                {stats.reviewedCount}
              </p>
            </div>
            <div className="stat-card">
              <span className="text-xs text-warmgray-500">Actioned</span>
              <p className="text-xl font-bold text-red-600">
                {stats.actionedCount}
              </p>
            </div>
            <div className="stat-card">
              <span className="text-xs text-warmgray-500">Dismissed</span>
              <p className="text-xl font-bold text-warmgray-600">
                {stats.dismissedCount}
              </p>
            </div>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        {/* Reports table */}
        <div className="card p-0 overflow-hidden">
          {loading ? (
            <div className="text-center py-12 text-warmgray-500 text-sm">
              Loading reports...
            </div>
          ) : reports.length === 0 ? (
            <div className="text-center py-12 text-warmgray-400 text-sm">
              No pending reports
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-warmgray-200 bg-warmgray-50">
                      <th className="table-header">Reporter</th>
                      <th className="table-header">Content Type</th>
                      <th className="table-header">Reason</th>
                      <th className="table-header">Status</th>
                      <th className="table-header">Date</th>
                      <th className="table-header">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-warmgray-100">
                    {reports.map((report) => (
                      <tr key={report.id} className="hover:bg-warmgray-50">
                        <td className="table-cell">
                          <span className="text-xs font-mono text-warmgray-500">
                            {report.reporterUserId.slice(0, 8)}...
                          </span>
                        </td>
                        <td className="table-cell">
                          <span className="badge-blue text-xs">
                            {report.contentType}
                          </span>
                        </td>
                        <td className="table-cell">
                          <span className="font-medium">{report.reason}</span>
                          {report.description && (
                            <p className="text-xs text-warmgray-500 mt-0.5 max-w-xs truncate">
                              {report.description}
                            </p>
                          )}
                        </td>
                        <td className="table-cell">
                          <span
                            className={
                              STATUS_BADGE[report.status] ?? "badge-gray"
                            }
                          >
                            {report.status}
                          </span>
                        </td>
                        <td className="table-cell text-xs">
                          {new Date(report.createdAt).toLocaleDateString()}
                        </td>
                        <td className="table-cell">
                          <div className="flex gap-2">
                            <button
                              onClick={() => {
                                setModalReport(report);
                                setModalAction("Warning");
                                setModalNotes("");
                              }}
                              className="btn-primary text-xs py-1 px-2"
                            >
                              Review
                            </button>
                            <button
                              onClick={() => handleDismiss(report)}
                              className="btn-secondary text-xs py-1 px-2"
                            >
                              Dismiss
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between px-4 py-3 border-t border-warmgray-200">
                  <span className="text-xs text-warmgray-500">
                    Page {page} of {totalPages} ({totalCount} reports)
                  </span>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      disabled={page <= 1}
                      className="btn-secondary text-xs"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      disabled={page >= totalPages}
                      className="btn-secondary text-xs"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>

        {/* Review Modal */}
        {modalReport && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6">
              <h3 className="text-lg font-semibold text-warmgray-900 mb-4">
                Review Report
              </h3>

              <div className="space-y-3 mb-4">
                <div>
                  <span className="text-xs text-warmgray-500">Content Type</span>
                  <p className="text-sm font-medium">
                    {modalReport.contentType}
                  </p>
                </div>
                <div>
                  <span className="text-xs text-warmgray-500">Reason</span>
                  <p className="text-sm font-medium">{modalReport.reason}</p>
                </div>
                {modalReport.description && (
                  <div>
                    <span className="text-xs text-warmgray-500">
                      Description
                    </span>
                    <p className="text-sm">{modalReport.description}</p>
                  </div>
                )}
              </div>

              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-warmgray-700 mb-1">
                    Action
                  </label>
                  <select
                    value={modalAction}
                    onChange={(e) => setModalAction(e.target.value)}
                    className="input-field text-sm"
                  >
                    {ACTION_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-warmgray-700 mb-1">
                    Notes
                  </label>
                  <textarea
                    value={modalNotes}
                    onChange={(e) => setModalNotes(e.target.value)}
                    rows={3}
                    className="input-field text-sm resize-none"
                    placeholder="Optional review notes..."
                  />
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={() => setModalReport(null)}
                  className="btn-secondary flex-1"
                  disabled={modalLoading}
                >
                  Cancel
                </button>
                <button
                  onClick={handleReview}
                  className="btn-primary flex-1"
                  disabled={modalLoading}
                >
                  {modalLoading ? "Submitting..." : "Submit Review"}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}
