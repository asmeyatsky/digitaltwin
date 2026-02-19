"use client";

import { useEffect, useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { getTherapists, TherapistProfile } from "@/lib/api";

export default function TherapistsPage() {
  const [therapists, setTherapists] = useState<TherapistProfile[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterVerified, setFilterVerified] = useState<"all" | "verified" | "unverified">("all");

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const res = await getTherapists(page);
        setTherapists(res.data.therapists);
        setTotalCount(res.data.totalCount);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load therapists");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [page]);

  const filtered = therapists.filter((t) => {
    if (filterVerified === "verified") return t.isVerified;
    if (filterVerified === "unverified") return !t.isVerified;
    return true;
  });

  function parseSpecializations(json: string): string[] {
    try {
      return JSON.parse(json);
    } catch {
      return [];
    }
  }

  const totalPages = Math.ceil(totalCount / 20);

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold text-warmgray-900">
              Therapist Management
            </h2>
            <p className="text-warmgray-500 mt-1">
              Review and manage therapist profiles
            </p>
          </div>
          <div className="flex gap-2">
            {(["all", "verified", "unverified"] as const).map((f) => (
              <button
                key={f}
                onClick={() => setFilterVerified(f)}
                className={`px-3 py-1.5 text-xs rounded-lg font-medium transition-colors ${
                  filterVerified === f
                    ? "bg-primary-500 text-white"
                    : "bg-white text-warmgray-600 border border-warmgray-300 hover:bg-warmgray-50"
                }`}
              >
                {f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
          </div>
        </div>

        {/* Summary */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">Total</span>
            <p className="text-2xl font-bold text-warmgray-800">
              {loading ? "--" : totalCount}
            </p>
          </div>
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">Verified</span>
            <p className="text-2xl font-bold text-green-600">
              {loading ? "--" : therapists.filter((t) => t.isVerified).length}
            </p>
          </div>
          <div className="stat-card">
            <span className="text-sm text-warmgray-500">Pending Verification</span>
            <p className="text-2xl font-bold text-amber-600">
              {loading ? "--" : therapists.filter((t) => !t.isVerified).length}
            </p>
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        {/* Therapist table */}
        <div className="card p-0 overflow-hidden">
          {loading ? (
            <div className="text-center py-12 text-warmgray-500 text-sm">
              Loading therapists...
            </div>
          ) : filtered.length === 0 ? (
            <div className="text-center py-12 text-warmgray-400 text-sm">
              No therapists found
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-warmgray-200 bg-warmgray-50">
                      <th className="table-header">Name</th>
                      <th className="table-header">Credentials</th>
                      <th className="table-header">Specializations</th>
                      <th className="table-header">Rate</th>
                      <th className="table-header">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-warmgray-100">
                    {filtered.map((t) => {
                      const specs = parseSpecializations(t.specializations);
                      return (
                        <tr key={t.id} className="hover:bg-warmgray-50">
                          <td className="table-cell">
                            <div>
                              <p className="font-medium text-warmgray-900">
                                {t.name}
                              </p>
                              <p className="text-xs text-warmgray-500 truncate max-w-xs">
                                {t.bio}
                              </p>
                            </div>
                          </td>
                          <td className="table-cell text-sm">{t.credentials}</td>
                          <td className="table-cell">
                            <div className="flex flex-wrap gap-1">
                              {specs.slice(0, 3).map((s) => (
                                <span key={s} className="badge-blue text-[10px]">
                                  {s}
                                </span>
                              ))}
                              {specs.length > 3 && (
                                <span className="badge-gray text-[10px]">
                                  +{specs.length - 3}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="table-cell">
                            ${t.ratePerSession.toFixed(0)}/session
                          </td>
                          <td className="table-cell">
                            {t.isVerified ? (
                              <span className="badge-green">Verified</span>
                            ) : (
                              <span className="badge-yellow">Unverified</span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between px-4 py-3 border-t border-warmgray-200">
                  <span className="text-xs text-warmgray-500">
                    Page {page} of {totalPages} ({totalCount} therapists)
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
      </div>
    </DashboardLayout>
  );
}
