import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useLocalSearchParams } from "expo-router";
import type { CommunityGroup, CommunityPost } from "@/lib/api";
import {
  getCommunityGroupById,
  getCommunityPosts,
  joinCommunityGroup,
  leaveCommunityGroup,
  getMyCommunityGroups,
} from "@/lib/api";

const CATEGORY_COLORS: Record<string, string> = {
  Support: "#EF4444",
  Interest: "#3B82F6",
  Wellness: "#10B981",
  Mindfulness: "#8B5CF6",
  Relationships: "#F59E0B",
};

function PostCard({
  post,
  onPress,
}: {
  post: CommunityPost;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-3"
      style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
    >
      <Text className="text-base font-bold text-warmgray-800 mb-1">
        {post.title}
      </Text>
      <Text className="text-sm text-warmgray-500 mb-2" numberOfLines={3}>
        {post.content}
      </Text>
      <View className="flex-row items-center">
        <Text className="text-xs text-warmgray-400 mr-4">
          {post.isAnonymous ? "Anonymous" : post.authorUserId.substring(0, 8) + "..."}
        </Text>
        <Text className="text-xs text-warmgray-400 mr-4">
          {post.likeCount} like{post.likeCount !== 1 ? "s" : ""}
        </Text>
        <Text className="text-xs text-warmgray-400">
          {post.replyCount} repl{post.replyCount !== 1 ? "ies" : "y"}
        </Text>
      </View>
    </Pressable>
  );
}

export default function GroupDetailScreen() {
  const router = useRouter();
  const { groupId } = useLocalSearchParams<{ groupId: string }>();
  const [group, setGroup] = useState<CommunityGroup | null>(null);
  const [posts, setPosts] = useState<CommunityPost[]>([]);
  const [isMember, setIsMember] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const fetchData = useCallback(async () => {
    if (!groupId) return;
    try {
      const [groupRes, postsRes, myGroupsRes] = await Promise.all([
        getCommunityGroupById(groupId),
        getCommunityPosts(groupId, 1, 20),
        getMyCommunityGroups(),
      ]);
      setGroup(groupRes.data ?? null);
      setPosts(postsRes.data?.posts ?? []);
      setTotalCount(postsRes.data?.totalCount ?? 0);
      setPage(1);

      const memberGroupIds = (myGroupsRes.data?.groups ?? []).map((g) => g.id);
      setIsMember(memberGroupIds.includes(groupId));
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [groupId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchData();
    setRefreshing(false);
  };

  const handleJoin = async () => {
    if (!groupId) return;
    try {
      await joinCommunityGroup(groupId);
      setIsMember(true);
      await fetchData();
    } catch {
      // ignore
    }
  };

  const handleLeave = async () => {
    if (!groupId) return;
    try {
      await leaveCommunityGroup(groupId);
      setIsMember(false);
      await fetchData();
    } catch {
      // ignore
    }
  };

  if (loading) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <ActivityIndicator size="large" color="#FF8B47" />
      </SafeAreaView>
    );
  }

  if (!group) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <Text className="text-warmgray-500">Group not found</Text>
      </SafeAreaView>
    );
  }

  const color = CATEGORY_COLORS[group.category] ?? "#A89885";

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 100 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Group Info Header */}
        <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4">
          <View className="flex-row items-center mb-2">
            <View
              className="px-2 py-0.5 rounded-lg mr-2"
              style={{ backgroundColor: color + "20" }}
            >
              <Text className="text-xs font-semibold" style={{ color }}>
                {group.category}
              </Text>
            </View>
            {group.isModerated && (
              <View className="px-2 py-0.5 rounded-lg bg-warmgray-100">
                <Text className="text-xs text-warmgray-500">Moderated</Text>
              </View>
            )}
          </View>
          <Text className="text-xl font-bold text-warmgray-800 mb-1">
            {group.name}
          </Text>
          <Text className="text-sm text-warmgray-500 mb-3">
            {group.description}
          </Text>
          <Text className="text-xs text-warmgray-400 mb-3">
            {group.memberCount} member{group.memberCount !== 1 ? "s" : ""}
          </Text>

          {isMember ? (
            <Pressable
              onPress={handleLeave}
              className="border border-warmgray-200 rounded-xl py-2.5 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-warmgray-500 font-semibold text-sm">
                Leave Group
              </Text>
            </Pressable>
          ) : (
            <Pressable
              onPress={handleJoin}
              className="bg-primary-500 rounded-xl py-2.5 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-white font-semibold text-sm">
                Join Group
              </Text>
            </Pressable>
          )}
        </View>

        {/* Posts Feed */}
        <View className="flex-row items-center justify-between mb-3">
          <Text className="text-lg font-bold text-warmgray-800">
            Posts ({totalCount})
          </Text>
        </View>

        {posts.length > 0 ? (
          posts.map((post) => (
            <PostCard
              key={post.id}
              post={post}
              onPress={() => router.push(`/community/post/${post.id}`)}
            />
          ))
        ) : (
          <View className="items-center py-8">
            <Text className="text-warmgray-400 text-sm">
              {isMember
                ? "No posts yet. Be the first to share!"
                : "Join this group to see and create posts"}
            </Text>
          </View>
        )}
      </ScrollView>

      {/* FAB to create post */}
      {isMember && (
        <Pressable
          onPress={() => router.push(`/community/create-post?groupId=${groupId}`)}
          className="absolute bottom-6 right-6 w-14 h-14 rounded-full bg-primary-500 items-center justify-center"
          style={({ pressed }) => ({
            opacity: pressed ? 0.8 : 1,
            shadowColor: "#000",
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.25,
            shadowRadius: 4,
            elevation: 5,
          })}
        >
          <Text className="text-white text-2xl font-bold" style={{ marginTop: -2 }}>
            +
          </Text>
        </Pressable>
      )}
    </SafeAreaView>
  );
}
