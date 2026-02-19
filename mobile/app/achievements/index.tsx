import React, { useState, useCallback } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import {
  getMyAchievements,
  type UserAchievement,
  type AchievementCategory,
} from "@/lib/api";

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const CATEGORIES: { label: string; value: AchievementCategory | "All" }[] = [
  { label: "All", value: "All" },
  { label: "Milestone", value: "Milestone" },
  { label: "Emotional", value: "Emotional" },
  { label: "Growth", value: "Growth" },
  { label: "Consistency", value: "Consistency" },
  { label: "Social", value: "Social" },
];

const CATEGORY_EMOJI: Record<AchievementCategory, string> = {
  Emotional: "\u2764\uFE0F",
  Social: "\uD83D\uDC65",
  Growth: "\uD83C\uDF31",
  Consistency: "\uD83D\uDD25",
  Milestone: "\u2B50",
};

const CATEGORY_COLORS: Record<AchievementCategory, string> = {
  Emotional: "#EC4899",
  Social: "#3B82F6",
  Growth: "#10B981",
  Consistency: "#F59E0B",
  Milestone: "#8B5CF6",
};

// ---------------------------------------------------------------------------
// Components
// ---------------------------------------------------------------------------

function AchievementCard({ achievement }: { achievement: UserAchievement }) {
  const emoji = CATEGORY_EMOJI[achievement.category] ?? "\u2B50";
  const color = CATEGORY_COLORS[achievement.category] ?? "#6B7280";
  const progressPct = Math.min(
    (achievement.progress / achievement.requiredCount) * 100,
    100
  );

  return (
    <View
      className="flex-1 bg-white rounded-2xl p-4 border"
      style={{
        borderColor: achievement.isUnlocked ? color : "#E5E0DB",
        borderWidth: achievement.isUnlocked ? 2 : 1,
        opacity: achievement.isUnlocked ? 1 : 0.55,
      }}
    >
      {/* Icon */}
      <View
        className="w-12 h-12 rounded-full items-center justify-center mb-3"
        style={{
          backgroundColor: achievement.isUnlocked
            ? color + "20"
            : "#F3F0ED",
        }}
      >
        <Text style={{ fontSize: 22 }}>{emoji}</Text>
      </View>

      {/* Title & description */}
      <Text
        className="text-sm font-bold text-warmgray-800 mb-1"
        numberOfLines={1}
      >
        {achievement.title}
      </Text>
      <Text
        className="text-xs text-warmgray-400 mb-3"
        numberOfLines={2}
      >
        {achievement.description}
      </Text>

      {/* Progress bar */}
      <View className="h-2 rounded-full bg-warmgray-100 overflow-hidden mb-1">
        <View
          className="h-full rounded-full"
          style={{
            width: `${progressPct}%`,
            backgroundColor: achievement.isUnlocked ? color : "#A8A29E",
          }}
        />
      </View>

      {/* Progress text */}
      <Text className="text-xs text-warmgray-400">
        {achievement.isUnlocked
          ? `Unlocked ${achievement.unlockedAt ? formatDate(achievement.unlockedAt) : ""}`
          : `${achievement.progress} / ${achievement.requiredCount}`}
      </Text>
    </View>
  );
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

// ---------------------------------------------------------------------------
// Main Screen
// ---------------------------------------------------------------------------

export default function AchievementsScreen() {
  const [achievements, setAchievements] = useState<UserAchievement[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<
    AchievementCategory | "All"
  >("All");

  const fetchAchievements = useCallback(async () => {
    try {
      const res = await getMyAchievements();
      if (res.success && res.data) {
        setAchievements(res.data);
      }
    } catch {
      // silently handle
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      fetchAchievements();
    }, [fetchAchievements])
  );

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchAchievements();
    setRefreshing(false);
  };

  const filtered =
    selectedCategory === "All"
      ? achievements
      : achievements.filter((a) => a.category === selectedCategory);

  const unlockedCount = achievements.filter((a) => a.isUnlocked).length;

  if (loading) {
    return (
      <SafeAreaView className="flex-1 bg-companion-bg items-center justify-center">
        <ActivityIndicator size="large" color="#8B5CF6" />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header summary */}
        <View className="mb-4">
          <Text className="text-sm text-warmgray-500">
            {unlockedCount} of {achievements.length} achievements unlocked
          </Text>
        </View>

        {/* Category filter tabs */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-4"
          contentContainerStyle={{ gap: 8 }}
        >
          {CATEGORIES.map((cat) => {
            const isActive = selectedCategory === cat.value;
            return (
              <Pressable
                key={cat.value}
                onPress={() => setSelectedCategory(cat.value)}
                className="rounded-full px-4 py-2"
                style={{
                  backgroundColor: isActive ? "#3D2E22" : "#FFFFFF",
                  borderWidth: 1,
                  borderColor: isActive ? "#3D2E22" : "#E5E0DB",
                }}
              >
                <Text
                  className="text-sm font-medium"
                  style={{ color: isActive ? "#FFFFFF" : "#78716C" }}
                >
                  {cat.label}
                </Text>
              </Pressable>
            );
          })}
        </ScrollView>

        {/* Achievement grid — 2 columns */}
        {filtered.length === 0 ? (
          <View className="items-center py-12">
            <Text className="text-warmgray-400 text-sm">
              No achievements in this category yet.
            </Text>
          </View>
        ) : (
          <View className="flex-row flex-wrap" style={{ gap: 12 }}>
            {filtered.map((a) => (
              <View key={a.key} style={{ width: "48%" }}>
                <AchievementCard achievement={a} />
              </View>
            ))}
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
