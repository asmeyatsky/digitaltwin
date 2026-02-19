import React, { useState, useCallback } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
  Alert,
  Platform,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useFocusEffect } from "expo-router";
import {
  getTimeline,
  deleteLifeEvent,
  type LifeEvent,
  type LifeEventCategory,
  type EmotionType,
} from "@/lib/api";

const CATEGORY_COLORS: Record<LifeEventCategory, string> = {
  Career: "#3B82F6",
  Relationship: "#EC4899",
  Health: "#10B981",
  Education: "#8B5CF6",
  Milestone: "#F59E0B",
  Loss: "#6B7280",
  Achievement: "#F97316",
  Travel: "#06B6D4",
};

const EMOTION_LABELS: Record<EmotionType, string> = {
  Neutral: "Neutral",
  Happy: "Happy",
  Sad: "Sad",
  Angry: "Angry",
  Anxious: "Anxious",
  Surprised: "Surprised",
  Calm: "Calm",
  Excited: "Excited",
};

const EMOTION_ICONS: Record<EmotionType, string> = {
  Neutral: "~",
  Happy: ":)",
  Sad: ":(",
  Angry: ">:",
  Anxious: ":S",
  Surprised: ":O",
  Calm: ":)",
  Excited: ":D",
};

export default function LifeTimelineScreen() {
  const router = useRouter();
  const [events, setEvents] = useState<LifeEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchEvents = useCallback(async () => {
    try {
      const res = await getTimeline();
      setEvents(res.data ?? []);
    } catch (err) {
      console.error("Failed to fetch timeline", err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      setLoading(true);
      fetchEvents();
    }, [fetchEvents])
  );

  const handleRefresh = () => {
    setRefreshing(true);
    fetchEvents();
  };

  const handleDelete = (event: LifeEvent) => {
    const doDelete = async () => {
      try {
        await deleteLifeEvent(event.id);
        setEvents((prev) => prev.filter((e) => e.id !== event.id));
      } catch (err) {
        console.error("Failed to delete event", err);
      }
    };

    if (Platform.OS === "web") {
      if (window.confirm(`Delete "${event.title}"?`)) {
        doDelete();
      }
    } else {
      Alert.alert("Delete Event", `Delete "${event.title}"?`, [
        { text: "Cancel", style: "cancel" },
        { text: "Delete", style: "destructive", onPress: doDelete },
      ]);
    }
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      {loading ? (
        <ActivityIndicator size="large" color="#FF8B47" className="mt-12" />
      ) : (
        <ScrollView
          contentContainerStyle={{ padding: 16, paddingBottom: 96 }}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
          }
        >
          <Text className="text-sm text-warmgray-500 mb-4">
            Your personal timeline of life events. Tap the + button below to add
            a new event.
          </Text>

          {events.length === 0 ? (
            <View className="items-center mt-12">
              <Text className="text-warmgray-400 text-base text-center">
                No life events yet.
              </Text>
              <Text className="text-warmgray-400 text-sm text-center mt-1">
                Add your first event to start building your timeline.
              </Text>
            </View>
          ) : (
            <View className="gap-3">
              {events.map((event) => {
                const catColor =
                  CATEGORY_COLORS[event.category] ?? "#9CA3AF";
                const emotionLabel =
                  EMOTION_LABELS[event.emotionalImpact] ?? event.emotionalImpact;
                const emotionIcon =
                  EMOTION_ICONS[event.emotionalImpact] ?? "";

                return (
                  <Pressable
                    key={event.id}
                    onLongPress={() => handleDelete(event)}
                    className="bg-white rounded-2xl px-4 py-4 border border-warmgray-100"
                  >
                    {/* Header: date + recurring badge */}
                    <View className="flex-row items-center justify-between mb-1">
                      <Text className="text-xs text-warmgray-400">
                        {formatDate(event.eventDate)}
                      </Text>
                      {event.isRecurring && (
                        <View className="bg-warmgray-100 rounded-full px-2 py-0.5">
                          <Text className="text-xs text-warmgray-500">
                            Recurring
                          </Text>
                        </View>
                      )}
                    </View>

                    {/* Title */}
                    <Text className="text-base font-semibold text-warmgray-800">
                      {event.title}
                    </Text>

                    {/* Description */}
                    {event.description ? (
                      <Text
                        className="text-sm text-warmgray-500 mt-1"
                        numberOfLines={2}
                      >
                        {event.description}
                      </Text>
                    ) : null}

                    {/* Footer: category badge + emotion */}
                    <View className="flex-row items-center gap-2 mt-2">
                      <View
                        style={{ backgroundColor: catColor + "20" }}
                        className="rounded-full px-2.5 py-0.5"
                      >
                        <Text style={{ color: catColor }} className="text-xs font-medium">
                          {event.category}
                        </Text>
                      </View>
                      <Text className="text-xs text-warmgray-400">
                        {emotionIcon} {emotionLabel}
                      </Text>
                    </View>
                  </Pressable>
                );
              })}
            </View>
          )}
        </ScrollView>
      )}

      {/* Floating Action Button */}
      <Pressable
        onPress={() => router.push("/life/add-event")}
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
    </SafeAreaView>
  );
}
