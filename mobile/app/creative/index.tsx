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
import type { CreativeWork, CreativeWorkType, EmotionType } from "@/lib/api";
import { getCreativeWorks, deleteCreativeWork } from "@/lib/api";

const WORK_TYPES: CreativeWorkType[] = [
  "Story",
  "Poem",
  "Reflection",
  "Gratitude",
  "Letter",
  "FreeWrite",
];

const TYPE_LABELS: Record<CreativeWorkType, string> = {
  Story: "Story",
  Poem: "Poem",
  Reflection: "Reflection",
  Gratitude: "Gratitude",
  Letter: "Letter",
  FreeWrite: "Free Write",
};

const TYPE_COLORS: Record<CreativeWorkType, string> = {
  Story: "#3B82F6",
  Poem: "#8B5CF6",
  Reflection: "#10B981",
  Gratitude: "#F59E0B",
  Letter: "#EF4444",
  FreeWrite: "#6366F1",
};

const MOOD_COLORS: Record<EmotionType, string> = {
  Neutral: "#9CA3AF",
  Happy: "#FCD34D",
  Sad: "#60A5FA",
  Angry: "#F87171",
  Anxious: "#FB923C",
  Surprised: "#A78BFA",
  Calm: "#6EE7B7",
  Excited: "#F472B6",
};

function WorkCard({
  work,
  onPress,
}: {
  work: CreativeWork;
  onPress: () => void;
}) {
  const typeColor = TYPE_COLORS[work.type] ?? "#A89885";
  const moodColor = MOOD_COLORS[work.mood as EmotionType] ?? "#9CA3AF";

  return (
    <Pressable
      onPress={onPress}
      className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-3"
      style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
    >
      <View className="flex-row items-center mb-2">
        <View
          className="px-2 py-0.5 rounded-lg mr-2"
          style={{ backgroundColor: typeColor + "20" }}
        >
          <Text className="text-xs font-semibold" style={{ color: typeColor }}>
            {TYPE_LABELS[work.type]}
          </Text>
        </View>
        <View
          className="w-3 h-3 rounded-full mr-2"
          style={{ backgroundColor: moodColor }}
        />
        <Text className="text-xs text-warmgray-400 flex-1">
          {work.mood}
        </Text>
        {work.isShared && (
          <View className="px-2 py-0.5 rounded-lg bg-primary-50">
            <Text className="text-xs text-primary-600 font-medium">Shared</Text>
          </View>
        )}
      </View>
      <Text className="text-base font-bold text-warmgray-800 mb-1" numberOfLines={1}>
        {work.title || "Untitled"}
      </Text>
      <Text className="text-sm text-warmgray-500" numberOfLines={2}>
        {work.content}
      </Text>
      <Text className="text-xs text-warmgray-300 mt-2">
        {new Date(work.updatedAt).toLocaleDateString()}
      </Text>
    </Pressable>
  );
}

export default function CreativeIndexScreen() {
  const router = useRouter();
  const [works, setWorks] = useState<CreativeWork[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedType, setSelectedType] = useState<CreativeWorkType | null>(null);

  const fetchData = useCallback(async () => {
    try {
      const res = await getCreativeWorks(selectedType ?? undefined, 1, 50);
      setWorks(res.data?.works ?? []);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [selectedType]);

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

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 100 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header */}
        <View className="mb-4">
          <Text className="text-2xl font-bold text-warmgray-800">
            Creative Studio
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Express yourself through writing and creative reflection
          </Text>
        </View>

        {/* Quick Actions */}
        <View className="flex-row gap-3 mb-4">
          <Pressable
            onPress={() => router.push("/creative/prompts")}
            className="flex-1 bg-primary-500 rounded-xl py-3 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
          >
            <Text className="text-white font-semibold text-sm">Get a Prompt</Text>
          </Pressable>
        </View>

        {/* Type Filter Tabs */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-4"
          contentContainerStyle={{ gap: 8 }}
        >
          <Pressable
            onPress={() => setSelectedType(null)}
            className="px-4 py-1.5 rounded-full"
            style={{
              backgroundColor: !selectedType ? "#3D2E22" : "#F5F0EB",
            }}
          >
            <Text
              className="text-sm font-semibold"
              style={{ color: !selectedType ? "#fff" : "#A89885" }}
            >
              All
            </Text>
          </Pressable>
          {WORK_TYPES.map((t) => (
            <Pressable
              key={t}
              onPress={() => setSelectedType(selectedType === t ? null : t)}
              className="px-4 py-1.5 rounded-full"
              style={{
                backgroundColor:
                  selectedType === t
                    ? TYPE_COLORS[t]
                    : (TYPE_COLORS[t] ?? "#A89885") + "15",
              }}
            >
              <Text
                className="text-sm font-semibold"
                style={{
                  color: selectedType === t ? "#fff" : TYPE_COLORS[t],
                }}
              >
                {TYPE_LABELS[t]}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* Works List */}
        {works.length > 0 ? (
          works.map((work) => (
            <WorkCard
              key={work.id}
              work={work}
              onPress={() =>
                router.push({
                  pathname: "/creative/editor",
                  params: { workId: work.id },
                })
              }
            />
          ))
        ) : (
          <View className="items-center py-12">
            <Text className="text-warmgray-400 text-base mb-2">
              No creative works yet
            </Text>
            <Text className="text-warmgray-300 text-sm text-center">
              Tap the button below to start writing
            </Text>
          </View>
        )}
      </ScrollView>

      {/* FAB to create new work */}
      <Pressable
        onPress={() => router.push("/creative/editor")}
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
        <Text className="text-white text-2xl font-light">+</Text>
      </Pressable>
    </SafeAreaView>
  );
}
