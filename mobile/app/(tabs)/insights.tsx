import React, { useState } from "react";
import { View, Text, ScrollView, Pressable, RefreshControl } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useEmotion, EMOTION_COLORS, getEmotionColor } from "@/lib/hooks";
import type { EmotionInsights } from "@/lib/api";

// ---------------------------------------------------------------------------
// Mini chart components (pure RN, no external charting library)
// ---------------------------------------------------------------------------

function EmotionRingChart({
  distribution,
}: {
  distribution: Record<string, number>;
}) {
  const entries = Object.entries(distribution).sort((a, b) => b[1] - a[1]);
  const total = entries.reduce((sum, [, v]) => sum + v, 0);
  if (total === 0) return null;

  // We'll render a simple horizontal stacked bar as the ring chart representation
  // (a true SVG ring would require react-native-svg which is available but keeping it simple)
  return (
    <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
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

      {/* Legend */}
      <View className="flex-row flex-wrap gap-3">
        {entries.slice(0, 6).map(([emotion, count]) => {
          const pct = Math.round((count / total) * 100);
          return (
            <View key={emotion} className="flex-row items-center gap-1.5">
              <View
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: getEmotionColor(emotion) }}
              />
              <Text className="text-xs text-warmgray-600 capitalize">
                {emotion} {pct}%
              </Text>
            </View>
          );
        })}
      </View>
    </View>
  );
}

function MoodTimeline({
  timeline,
}: {
  timeline: { date: string; valence: number }[];
}) {
  if (timeline.length === 0) return null;

  // Normalize valence (-1..1) to 0..1 for bar height
  const maxBars = 14;
  const data = timeline.slice(-maxBars);

  return (
    <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
      <Text className="text-lg font-bold text-warmgray-800 mb-4">
        Mood Over Time
      </Text>

      {/* Simple bar chart */}
      <View className="flex-row items-end justify-between h-32 gap-1">
        {data.map((point, i) => {
          const normalized = (point.valence + 1) / 2; // 0 to 1
          const height = Math.max(8, normalized * 100);
          const isPositive = point.valence >= 0;

          return (
            <View key={i} className="flex-1 items-center">
              <View
                style={{
                  height: `${height}%`,
                  backgroundColor: isPositive ? "#A8D8B9" : "#7BA7C9",
                  borderRadius: 4,
                  width: "100%",
                  minHeight: 8,
                }}
              />
            </View>
          );
        })}
      </View>

      {/* X-axis labels */}
      <View className="flex-row justify-between mt-2">
        {data.length > 0 && (
          <>
            <Text className="text-xs text-warmgray-400">
              {formatShortDate(data[0].date)}
            </Text>
            <Text className="text-xs text-warmgray-400">
              {formatShortDate(data[data.length - 1].date)}
            </Text>
          </>
        )}
      </View>

      {/* Legend */}
      <View className="flex-row gap-4 mt-3">
        <View className="flex-row items-center gap-1.5">
          <View className="w-3 h-3 rounded-full bg-companion-calm" />
          <Text className="text-xs text-warmgray-500">Positive</Text>
        </View>
        <View className="flex-row items-center gap-1.5">
          <View className="w-3 h-3 rounded-full bg-companion-sadness" />
          <Text className="text-xs text-warmgray-500">Negative</Text>
        </View>
      </View>
    </View>
  );
}

function StatCard({
  label,
  value,
  sub,
}: {
  label: string;
  value: string;
  sub?: string;
}) {
  return (
    <View className="flex-1 bg-white rounded-3xl p-4 border border-warmgray-100 items-center">
      <Text className="text-2xl font-bold text-warmgray-800">{value}</Text>
      <Text className="text-sm text-warmgray-500 mt-1">{label}</Text>
      {sub && (
        <Text className="text-xs text-warmgray-400 mt-0.5">{sub}</Text>
      )}
    </View>
  );
}

function TopEmotions({
  emotions,
}: {
  emotions: { emotion: string; count: number; percentage: number }[];
}) {
  if (emotions.length === 0) return null;

  return (
    <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
      <Text className="text-lg font-bold text-warmgray-800 mb-4">
        Most Frequent Emotions
      </Text>

      <View className="gap-3">
        {emotions.slice(0, 5).map((item, i) => (
          <View key={item.emotion} className="flex-row items-center gap-3">
            <Text className="text-sm font-bold text-warmgray-400 w-5">
              {i + 1}
            </Text>
            <View
              className="w-8 h-8 rounded-full items-center justify-center"
              style={{
                backgroundColor: getEmotionColor(item.emotion) + "25",
              }}
            >
              <View
                className="w-4 h-4 rounded-full"
                style={{
                  backgroundColor: getEmotionColor(item.emotion),
                }}
              />
            </View>
            <View className="flex-1">
              <Text className="text-sm font-semibold text-warmgray-700 capitalize">
                {item.emotion}
              </Text>
              <View className="flex-row items-center mt-1">
                <View className="flex-1 h-2 rounded-full bg-warmgray-100 overflow-hidden">
                  <View
                    className="h-full rounded-full"
                    style={{
                      width: `${item.percentage}%`,
                      backgroundColor: getEmotionColor(item.emotion),
                    }}
                  />
                </View>
                <Text className="text-xs text-warmgray-400 ml-2 w-10 text-right">
                  {Math.round(item.percentage)}%
                </Text>
              </View>
            </View>
          </View>
        ))}
      </View>
    </View>
  );
}

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------

function formatShortDate(dateStr: string): string {
  const d = new Date(dateStr);
  return `${d.getMonth() + 1}/${d.getDate()}`;
}

// ---------------------------------------------------------------------------
// Placeholder data for when API is unavailable
// ---------------------------------------------------------------------------

const PLACEHOLDER_INSIGHTS: EmotionInsights = {
  emotionDistribution: {
    calm: 32,
    joy: 28,
    sadness: 15,
    surprise: 10,
    love: 8,
    anger: 4,
    fear: 3,
  },
  moodTimeline: Array.from({ length: 14 }, (_, i) => ({
    date: new Date(Date.now() - (13 - i) * 86400000).toISOString(),
    valence: Math.sin(i * 0.5) * 0.6 + Math.random() * 0.3,
  })),
  sessionCount: 24,
  averageDurationMinutes: 12,
  topEmotions: [
    { emotion: "calm", count: 32, percentage: 32 },
    { emotion: "joy", count: 28, percentage: 28 },
    { emotion: "sadness", count: 15, percentage: 15 },
    { emotion: "surprise", count: 10, percentage: 10 },
    { emotion: "love", count: 8, percentage: 8 },
  ],
};

// ---------------------------------------------------------------------------
// Main Screen
// ---------------------------------------------------------------------------

export default function InsightsScreen() {
  const { insights, isLoadingInsights, refetchInsights } = useEmotion();
  const [refreshing, setRefreshing] = useState(false);

  const data = insights ?? PLACEHOLDER_INSIGHTS;

  const onRefresh = async () => {
    setRefreshing(true);
    await refetchInsights();
    setRefreshing(false);
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 32 }}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header */}
        <View className="mb-6">
          <Text className="text-2xl font-bold text-warmgray-800">
            Emotional Insights
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Understanding your emotional patterns
          </Text>
        </View>

        {/* Stats Row */}
        <View className="flex-row gap-3 mb-4">
          <StatCard
            label="Sessions"
            value={String(data.sessionCount)}
            sub="this week"
          />
          <StatCard
            label="Avg Duration"
            value={`${data.averageDurationMinutes}m`}
            sub="per session"
          />
        </View>

        {/* Charts */}
        <View className="gap-4">
          <EmotionRingChart distribution={data.emotionDistribution} />
          <MoodTimeline timeline={data.moodTimeline} />
          <TopEmotions emotions={data.topEmotions} />
        </View>

        {!insights && (
          <View className="mt-4 bg-primary-50 rounded-2xl px-4 py-3">
            <Text className="text-sm text-primary-700 text-center">
              Showing sample data. Start chatting to see your real insights!
            </Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
