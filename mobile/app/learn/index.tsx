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
import { useRouter } from "expo-router";
import type {
  LearningPath,
  LearningCategory,
  UserLearningProgress,
} from "@/lib/api";
import {
  getLearningPaths,
  getLearningProgress,
  getSuggestedLearningPath,
} from "@/lib/api";

const CATEGORIES: { label: string; value: LearningCategory }[] = [
  { label: "Emotional Intelligence", value: "EmotionalIntelligence" },
  { label: "Mindfulness", value: "Mindfulness" },
  { label: "Communication", value: "Communication" },
  { label: "Stress Management", value: "StressManagement" },
  { label: "Resilience", value: "Resilience" },
  { label: "Self-Care", value: "SelfCare" },
];

const CATEGORY_COLORS: Record<string, string> = {
  EmotionalIntelligence: "#8B5CF6",
  Mindfulness: "#10B981",
  Communication: "#3B82F6",
  StressManagement: "#EF4444",
  Resilience: "#F59E0B",
  SelfCare: "#EC4899",
};

const CATEGORY_LABELS: Record<string, string> = {
  EmotionalIntelligence: "Emotional Intelligence",
  Mindfulness: "Mindfulness",
  Communication: "Communication",
  StressManagement: "Stress Management",
  Resilience: "Resilience",
  SelfCare: "Self-Care",
};

function PathCard({
  path,
  progress,
  onPress,
}: {
  path: LearningPath;
  progress?: UserLearningProgress;
  onPress: () => void;
}) {
  const color = CATEGORY_COLORS[path.category] ?? "#A89885";
  const completedCount = progress
    ? JSON.parse(progress.completedModules || "[]").length
    : 0;
  const progressPercent =
    progress && path.moduleCount > 0
      ? Math.round((completedCount / path.moduleCount) * 100)
      : 0;
  const isCompleted = progress?.completedAt != null;

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
            {CATEGORY_LABELS[path.category] ?? path.category}
          </Text>
        </View>
        <Text className="text-xs text-warmgray-400">
          {path.moduleCount} module{path.moduleCount !== 1 ? "s" : ""} /{" "}
          ~{path.estimatedMinutes} min
        </Text>
      </View>
      <Text className="text-base font-bold text-warmgray-800 mb-1">
        {path.title}
      </Text>
      <Text className="text-sm text-warmgray-500 mb-3" numberOfLines={2}>
        {path.description}
      </Text>

      {/* Progress bar */}
      {progress && (
        <View className="mb-2">
          <View className="flex-row justify-between mb-1">
            <Text className="text-xs text-warmgray-500">
              {isCompleted
                ? "Completed"
                : `${completedCount} / ${path.moduleCount} modules`}
            </Text>
            <Text className="text-xs font-semibold" style={{ color }}>
              {progressPercent}%
            </Text>
          </View>
          <View className="h-2 bg-warmgray-100 rounded-full overflow-hidden">
            <View
              className="h-full rounded-full"
              style={{
                width: `${progressPercent}%`,
                backgroundColor: isCompleted ? "#10B981" : color,
              }}
            />
          </View>
        </View>
      )}

      <Pressable
        onPress={onPress}
        className="rounded-xl py-2 items-center"
        style={({ pressed }) => ({
          opacity: pressed ? 0.7 : 1,
          backgroundColor: progress ? color : "#3D2E22",
        })}
      >
        <Text className="text-white font-semibold text-sm">
          {isCompleted
            ? "Review"
            : progress
            ? "Continue"
            : "Start Learning"}
        </Text>
      </Pressable>
    </Pressable>
  );
}

export default function LearnScreen() {
  const router = useRouter();
  const [paths, setPaths] = useState<LearningPath[]>([]);
  const [progressMap, setProgressMap] = useState<
    Record<string, UserLearningProgress>
  >({});
  const [suggestedPath, setSuggestedPath] = useState<LearningPath | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedCategory, setSelectedCategory] =
    useState<LearningCategory | null>(null);

  const fetchData = useCallback(async () => {
    try {
      const [pathsRes, progressRes, suggestedRes] = await Promise.all([
        getLearningPaths(selectedCategory ?? undefined),
        getLearningProgress(),
        getSuggestedLearningPath(),
      ]);

      setPaths(pathsRes.data?.paths ?? []);

      const progressList = progressRes.data?.progress ?? [];
      const map: Record<string, UserLearningProgress> = {};
      for (const p of progressList) {
        map[p.pathId] = p;
      }
      setProgressMap(map);

      setSuggestedPath(suggestedRes.data ?? null);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [selectedCategory]);

  useEffect(() => {
    setLoading(true);
    fetchData();
  }, [fetchData]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchData();
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

  // Separate in-progress paths from others
  const inProgressPaths = paths.filter(
    (p) => progressMap[p.id] && !progressMap[p.id].completedAt
  );
  const availablePaths = paths.filter((p) => !progressMap[p.id]);
  const completedPaths = paths.filter(
    (p) => progressMap[p.id]?.completedAt != null
  );

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
          <Text className="text-2xl font-bold text-warmgray-800">Learn</Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Grow your emotional skills with guided learning paths
          </Text>
        </View>

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
              key={cat.value}
              onPress={() =>
                setSelectedCategory(
                  selectedCategory === cat.value ? null : cat.value
                )
              }
              className="px-4 py-1.5 rounded-full"
              style={{
                backgroundColor:
                  selectedCategory === cat.value
                    ? CATEGORY_COLORS[cat.value]
                    : (CATEGORY_COLORS[cat.value] ?? "#A89885") + "15",
              }}
            >
              <Text
                className="text-sm font-semibold"
                style={{
                  color:
                    selectedCategory === cat.value
                      ? "#fff"
                      : CATEGORY_COLORS[cat.value],
                }}
              >
                {cat.label}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* Suggested For You */}
        {suggestedPath && !selectedCategory && (
          <View className="mb-6">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Suggested For You
            </Text>
            <PathCard
              path={suggestedPath}
              progress={progressMap[suggestedPath.id]}
              onPress={() => router.push(`/learn/${suggestedPath.id}`)}
            />
          </View>
        )}

        {/* In Progress */}
        {inProgressPaths.length > 0 && (
          <View className="mb-6">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              In Progress
            </Text>
            {inProgressPaths.map((path) => (
              <PathCard
                key={path.id}
                path={path}
                progress={progressMap[path.id]}
                onPress={() => router.push(`/learn/${path.id}`)}
              />
            ))}
          </View>
        )}

        {/* Available Paths */}
        {availablePaths.length > 0 && (
          <View className="mb-6">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              {selectedCategory
                ? CATEGORY_LABELS[selectedCategory] ?? selectedCategory
                : "Available Paths"}
            </Text>
            {availablePaths.map((path) => (
              <PathCard
                key={path.id}
                path={path}
                onPress={() => router.push(`/learn/${path.id}`)}
              />
            ))}
          </View>
        )}

        {/* Completed */}
        {completedPaths.length > 0 && (
          <View className="mb-6">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Completed
            </Text>
            {completedPaths.map((path) => (
              <PathCard
                key={path.id}
                path={path}
                progress={progressMap[path.id]}
                onPress={() => router.push(`/learn/${path.id}`)}
              />
            ))}
          </View>
        )}

        {/* Empty state */}
        {paths.length === 0 && (
          <View className="items-center py-12">
            <Text className="text-warmgray-400 text-sm">
              No learning paths available yet
            </Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
