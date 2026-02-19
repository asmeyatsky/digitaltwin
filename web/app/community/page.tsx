"use client";

import { useEffect, useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import {
  getCommunityGroups,
  getCommunityPosts,
  CommunityGroup,
  CommunityPost,
} from "@/lib/api";

const CATEGORIES = [
  "All",
  "Support",
  "Interest",
  "Wellness",
  "Mindfulness",
  "Relationships",
];

export default function CommunityPage() {
  const [groups, setGroups] = useState<CommunityGroup[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [category, setCategory] = useState("All");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Posts panel
  const [selectedGroup, setSelectedGroup] = useState<CommunityGroup | null>(null);
  const [posts, setPosts] = useState<CommunityPost[]>([]);
  const [postsLoading, setPostsLoading] = useState(false);

  useEffect(() => {
    loadGroups();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, category]);

  async function loadGroups() {
    setLoading(true);
    setError(null);
    try {
      const cat = category === "All" ? undefined : category;
      const res = await getCommunityGroups(page, 20, cat, search || undefined);
      setGroups(res.data.groups);
      setTotalCount(res.data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load groups");
    } finally {
      setLoading(false);
    }
  }

  async function handleSelectGroup(group: CommunityGroup) {
    setSelectedGroup(group);
    setPostsLoading(true);
    try {
      const res = await getCommunityPosts(group.id);
      setPosts(res.data.posts);
    } catch {
      setPosts([]);
    } finally {
      setPostsLoading(false);
    }
  }

  function handleSearch() {
    setPage(1);
    loadGroups();
  }

  const totalPages = Math.ceil(totalCount / 20);

  return (
    <DashboardLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h2 className="text-2xl font-bold text-warmgray-900">Community</h2>
          <p className="text-warmgray-500 mt-1">
            Browse community groups and posts
          </p>
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex gap-2 flex-wrap">
            {CATEGORIES.map((cat) => (
              <button
                key={cat}
                onClick={() => {
                  setCategory(cat);
                  setPage(1);
                }}
                className={`px-3 py-1.5 text-xs rounded-lg font-medium transition-colors ${
                  category === cat
                    ? "bg-primary-500 text-white"
                    : "bg-white text-warmgray-600 border border-warmgray-300 hover:bg-warmgray-50"
                }`}
              >
                {cat}
              </button>
            ))}
          </div>
          <div className="flex gap-2 sm:ml-auto">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              placeholder="Search groups..."
              className="input-field text-sm w-48"
            />
            <button onClick={handleSearch} className="btn-primary text-sm">
              Search
            </button>
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Groups table */}
          <div className="lg:col-span-2 card p-0 overflow-hidden">
            {loading ? (
              <div className="text-center py-12 text-warmgray-500 text-sm">
                Loading groups...
              </div>
            ) : groups.length === 0 ? (
              <div className="text-center py-12 text-warmgray-400 text-sm">
                No groups found
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead>
                      <tr className="border-b border-warmgray-200 bg-warmgray-50">
                        <th className="table-header">Name</th>
                        <th className="table-header">Category</th>
                        <th className="table-header">Members</th>
                        <th className="table-header">Created</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-warmgray-100">
                      {groups.map((group) => (
                        <tr
                          key={group.id}
                          onClick={() => handleSelectGroup(group)}
                          className={`cursor-pointer hover:bg-warmgray-50 transition-colors ${
                            selectedGroup?.id === group.id
                              ? "bg-primary-50"
                              : ""
                          }`}
                        >
                          <td className="table-cell">
                            <div>
                              <p className="font-medium text-warmgray-900">
                                {group.name}
                              </p>
                              <p className="text-xs text-warmgray-500 truncate max-w-xs">
                                {group.description}
                              </p>
                            </div>
                          </td>
                          <td className="table-cell">
                            <span className="badge-blue">{group.category}</span>
                          </td>
                          <td className="table-cell">{group.memberCount}</td>
                          <td className="table-cell text-xs">
                            {new Date(group.createdAt).toLocaleDateString()}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center justify-between px-4 py-3 border-t border-warmgray-200">
                    <span className="text-xs text-warmgray-500">
                      Page {page} of {totalPages} ({totalCount} groups)
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

          {/* Posts panel */}
          <div className="card">
            {selectedGroup ? (
              <div>
                <h3 className="text-sm font-semibold text-warmgray-900 mb-1">
                  {selectedGroup.name}
                </h3>
                <p className="text-xs text-warmgray-500 mb-4">
                  {selectedGroup.description}
                </p>

                {postsLoading ? (
                  <p className="text-sm text-warmgray-400">Loading posts...</p>
                ) : posts.length === 0 ? (
                  <p className="text-sm text-warmgray-400">No posts yet</p>
                ) : (
                  <div className="space-y-3">
                    {posts.map((post) => (
                      <div
                        key={post.id}
                        className="border border-warmgray-200 rounded-lg p-3"
                      >
                        <h4 className="text-sm font-medium text-warmgray-900">
                          {post.title}
                        </h4>
                        <p className="text-xs text-warmgray-600 mt-1 line-clamp-3">
                          {post.content}
                        </p>
                        <div className="flex items-center gap-3 mt-2 text-xs text-warmgray-400">
                          <span>{post.likeCount} likes</span>
                          <span>{post.replyCount} replies</span>
                          <span>
                            {new Date(post.createdAt).toLocaleDateString()}
                          </span>
                          {post.isAnonymous && (
                            <span className="badge-gray text-[10px]">
                              Anonymous
                            </span>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ) : (
              <div className="text-center py-8 text-warmgray-400 text-sm">
                Select a group to view posts
              </div>
            )}
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
