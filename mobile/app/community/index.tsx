import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  TextInput,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import type { CommunityGroup, GroupCategory } from "@/lib/api";
import {
  getCommunityGroups,
  getMyCommunityGroups,
  getSuggestedCommunityGroups,
  joinCommunityGroup,
  leaveCommunityGroup,
} from "@/lib/api";

const CATEGORIES: GroupCategory[] = [
  "Support",
  "Interest",
  "Wellness",
  "Mindfulness",
  "Relationships",
];

const CATEGORY_COLORS: Record<string, string> = {
  Support: "#EF4444",
  Interest: "#3B82F6",
  Wellness: "#10B981",
  Mindfulness: "#8B5CF6",
  Relationships: "#F59E0B",
};

function GroupCard({
  group,
  isMember,
  onJoin,
  onLeave,
  onPress,
}: {
  group: CommunityGroup;
  isMember: boolean;
  onJoin: () => void;
  onLeave: () => void;
  onPress: () => void;
}) {
  const color = CATEGORY_COLORS[group.category] ?? "#A89885";

  return (
    <Pressable
      onPress={onPress}
      className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-3"
      style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
    >
      <View className="flex-row items-center mb-2">
        <View
          className="px-2 py-0.5 rounded-lg mr-2"
          style={{ backgroundColor: color + "20" }}
        >
          <Text className="text-xs font-semibold" style={{ color }}>
            {group.category}
          </Text>
        </View>
        <Text className="text-xs text-warmgray-400">
          {group.memberCount} member{group.memberCount !== 1 ? "s" : ""}
        </Text>
      </View>
      <Text className="text-base font-bold text-warmgray-800 mb-1">
        {group.name}
      </Text>
      <Text className="text-sm text-warmgray-500 mb-3" numberOfLines={2}>
        {group.description}
      </Text>
      {isMember ? (
        <View className="flex-row gap-2">
          <Pressable
            onPress={onPress}
            className="flex-1 bg-primary-500 rounded-xl py-2 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
          >
            <Text className="text-white font-semibold text-sm">View</Text>
          </Pressable>
          <Pressable
            onPress={onLeave}
            className="px-4 rounded-xl py-2 items-center border border-warmgray-200"
            style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
          >
            <Text className="text-warmgray-500 font-semibold text-sm">Leave</Text>
          </Pressable>
        </View>
      ) : (
        <Pressable
          onPress={onJoin}
          className="bg-warmgray-700 rounded-xl py-2 items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
        >
          <Text className="text-white font-semibold text-sm">Join Group</Text>
        </Pressable>
      )}
    </Pressable>
  );
}

export default function CommunityScreen() {
  const router = useRouter();
  const [myGroups, setMyGroups] = useState<CommunityGroup[]>([]);
  const [suggestedGroups, setSuggestedGroups] = useState<CommunityGroup[]>([]);
  const [searchResults, setSearchResults] = useState<CommunityGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [search, setSearch] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<GroupCategory | null>(null);
  const [searching, setSearching] = useState(false);

  const myGroupIds = new Set(myGroups.map((g) => g.id));

  const fetchData = useCallback(async () => {
    try {
      const [myRes, suggestedRes] = await Promise.all([
        getMyCommunityGroups(),
        getSuggestedCommunityGroups(),
      ]);
      setMyGroups(myRes.data?.groups ?? []);
      setSuggestedGroups(suggestedRes.data?.groups ?? []);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  useEffect(() => {
    const doSearch = async () => {
      if (!search.trim() && !selectedCategory) {
        setSearchResults([]);
        return;
      }
      setSearching(true);
      try {
        const res = await getCommunityGroups(
          selectedCategory ?? undefined,
          search.trim() || undefined,
          1,
          20
        );
        setSearchResults(res.data?.groups ?? []);
      } catch {
        // ignore
      } finally {
        setSearching(false);
      }
    };

    const timeout = setTimeout(doSearch, 400);
    return () => clearTimeout(timeout);
  }, [search, selectedCategory]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchData();
    setRefreshing(false);
  };

  const handleJoin = async (groupId: string) => {
    try {
      await joinCommunityGroup(groupId);
      await fetchData();
    } catch {
      // ignore
    }
  };

  const handleLeave = async (groupId: string) => {
    try {
      await leaveCommunityGroup(groupId);
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

  const showSearchResults = !!(search.trim() || selectedCategory);
  const displayGroups = showSearchResults ? searchResults : suggestedGroups;

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header */}
        <View className="mb-4">
          <Text className="text-2xl font-bold text-warmgray-800">
            Community
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Connect with peers for support and shared experiences
          </Text>
        </View>

        {/* Search Bar */}
        <TextInput
          className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-base text-warmgray-800 mb-3"
          placeholder="Search groups..."
          placeholderTextColor="#A89885"
          value={search}
          onChangeText={setSearch}
        />

        {/* Category Filter Tabs */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-4"
          contentContainerStyle={{ gap: 8 }}
        >
          <Pressable
            onPress={() => setSelectedCategory(null)}
            className="px-4 py-1.5 rounded-full"
            style={{
              backgroundColor: !selectedCategory ? "#3D2E22" : "#F5F0EB",
            }}
          >
            <Text
              className="text-sm font-semibold"
              style={{ color: !selectedCategory ? "#fff" : "#A89885" }}
            >
              All
            </Text>
          </Pressable>
          {CATEGORIES.map((cat) => (
            <Pressable
              key={cat}
              onPress={() =>
                setSelectedCategory(selectedCategory === cat ? null : cat)
              }
              className="px-4 py-1.5 rounded-full"
              style={{
                backgroundColor:
                  selectedCategory === cat
                    ? CATEGORY_COLORS[cat]
                    : (CATEGORY_COLORS[cat] ?? "#A89885") + "15",
              }}
            >
              <Text
                className="text-sm font-semibold"
                style={{
                  color: selectedCategory === cat ? "#fff" : CATEGORY_COLORS[cat],
                }}
              >
                {cat}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* My Groups Section */}
        {myGroups.length > 0 && !showSearchResults && (
          <View className="mb-6">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              My Groups
            </Text>
            {myGroups.map((group) => (
              <GroupCard
                key={group.id}
                group={group}
                isMember={true}
                onJoin={() => {}}
                onLeave={() => handleLeave(group.id)}
                onPress={() => router.push(`/community/${group.id}`)}
              />
            ))}
          </View>
        )}

        {/* Discover / Search Results */}
        <View>
          <Text className="text-lg font-bold text-warmgray-800 mb-3">
            {showSearchResults ? "Search Results" : "Discover Groups"}
          </Text>
          {searching ? (
            <ActivityIndicator size="small" color="#FF8B47" />
          ) : displayGroups.length > 0 ? (
            displayGroups.map((group) => (
              <GroupCard
                key={group.id}
                group={group}
                isMember={myGroupIds.has(group.id)}
                onJoin={() => handleJoin(group.id)}
                onLeave={() => handleLeave(group.id)}
                onPress={() => router.push(`/community/${group.id}`)}
              />
            ))
          ) : (
            <View className="items-center py-8">
              <Text className="text-warmgray-400 text-sm">
                {showSearchResults
                  ? "No groups match your search"
                  : "No suggested groups available"}
              </Text>
            </View>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
