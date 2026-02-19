import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useLocalSearchParams } from "expo-router";
import { getEmotionColor } from "@/lib/hooks";
import type { FamilyInsights } from "@/lib/api";
import { getFamilyInsights } from "@/lib/api";

const MOOD_LABELS: Record<string, { label: string; color: string }> = {
  happy: { label: "Happy", color: "#FFD700" },
  calm: { label: "Calm", color: "#A8D8B9" },
  sad: { label: "Sad", color: "#7BA7C9" },
  angry: { label: "Angry", color: "#E57373" },
  anxious: { label: "Anxious", color: "#FFB74D" },
  excited: { label: "Excited", color: "#FF8B47" },
  neutral: { label: "Neutral", color: "#A89885" },
  surprised: { label: "Surprised", color: "#CE93D8" },
};

function getMoodInfo(mood: string) {
  return (
    MOOD_LABELS[mood.toLowerCase()] ?? {
      label: mood.charAt(0).toUpperCase() + mood.slice(1),
      color: "#A89885",
    }
  );
}

export default function FamilyInsightsScreen() {
  const { familyId } = useLocalSearchParams<{ familyId: string }>();
  const [insights, setInsights] = useState<FamilyInsights | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchInsights = useCallback(async () => {
    if (!familyId) return;
    try {
      const res = await getFamilyInsights(familyId);
      setInsights(res.data);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [familyId]);

  useEffect(() => {
    fetchInsights();
  }, [fetchInsights]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchInsights();
    setRefreshing(false);
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

  if (!insights) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <Text className="text-warmgray-500 text-base">
          No insights available
        </Text>
      </SafeAreaView>
    );
  }

  const entries = Object.entries(insights.emotionDistribution).sort(
    (a, b) => b[1] - a[1]
  );
  const total = entries.reduce((sum, [, v]) => sum + v, 0);
  const moodInfo = getMoodInfo(insights.overallMood);

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        <View className="mb-6">
          <Text className="text-2xl font-bold text-warmgray-800">
            Family Emotional Insights
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Aggregated trends for the last 30 days
          </Text>
        </View>

        {/* Overall Mood */}
        <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4 items-center">
          <Text className="text-sm text-warmgray-500 mb-2">
            Overall Family Mood
          </Text>
          <View
            className="w-20 h-20 rounded-full items-center justify-center mb-3"
            style={{ backgroundColor: moodInfo.color + "30" }}
          >
            <View
              className="w-12 h-12 rounded-full"
              style={{ backgroundColor: moodInfo.color }}
            />
          </View>
          <Text className="text-xl font-bold text-warmgray-800 capitalize">
            {moodInfo.label}
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            {insights.memberCount} family member
            {insights.memberCount !== 1 ? "s" : ""}
          </Text>
        </View>

        {/* Emotion Distribution */}
        {total > 0 && (
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4">
            <Text className="text-lg font-bold text-warmgray-800 mb-4">
              Emotion Distribution
            </Text>

            {/* Stacked bar */}
            <View className="flex-row h-6 rounded-full overflow-hidden mb-4">
              {entries.map(([emotion, count]) => {
                const pct = (count / total) * 100;
                if (pct < 1) return null;
                return (
                  <View
                    key={emotion}
                    style={{
                      width: `${pct}%`,
                      backgroundColor: getEmotionColor(emotion),
                    }}
                  />
                );
              })}
            </View>

            {/* Emotion Rows */}
            <View className="gap-3">
              {entries.map(([emotion, count]) => {
                const pct = total > 0 ? Math.round((count / total) * 100) : 0;
                return (
                  <View
                    key={emotion}
                    className="flex-row items-center gap-3"
                  >
                    <View
                      className="w-3 h-3 rounded-full"
                      style={{ backgroundColor: getEmotionColor(emotion) }}
                    />
                    <Text className="flex-1 text-sm text-warmgray-700 capitalize">
                      {emotion}
                    </Text>
                    <Text className="text-sm text-warmgray-500">{count}</Text>
                    <Text className="text-xs text-warmgray-400 w-10 text-right">
                      {pct}%
                    </Text>
                  </View>
                );
              })}
            </View>
          </View>
        )}

        {total === 0 && (
          <View className="bg-primary-50 rounded-2xl px-4 py-3">
            <Text className="text-sm text-primary-700 text-center">
              No emotional data yet. Family members need to start chatting to
              generate insights.
            </Text>
          </View>
        )}

        {/* Period info */}
        <View className="mt-2 flex-row justify-between px-1">
          <Text className="text-xs text-warmgray-400">
            From: {new Date(insights.periodStart).toLocaleDateString()}
          </Text>
          <Text className="text-xs text-warmgray-400">
            To: {new Date(insights.periodEnd).toLocaleDateString()}
          </Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
