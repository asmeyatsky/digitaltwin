import React, { useEffect, useState } from "react";
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  ActivityIndicator,
  Alert,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useLocalSearchParams } from "expo-router";
import type { CreativeWorkType, EmotionType } from "@/lib/api";
import {
  createCreativeWork,
  updateCreativeWork,
  getCreativeWorkById,
  deleteCreativeWork,
  shareCreativeWork,
} from "@/lib/api";

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

const MOODS: EmotionType[] = [
  "Neutral",
  "Happy",
  "Sad",
  "Angry",
  "Anxious",
  "Surprised",
  "Calm",
  "Excited",
];

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

export default function CreativeEditorScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{
    workId?: string;
    prefillContent?: string;
    prefillType?: string;
  }>();

  const isEditing = !!params.workId;

  const [title, setTitle] = useState("");
  const [content, setContent] = useState(params.prefillContent ?? "");
  const [type, setType] = useState<CreativeWorkType>(
    (params.prefillType as CreativeWorkType) ?? "FreeWrite"
  );
  const [mood, setMood] = useState<EmotionType>("Neutral");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (params.workId) {
      setLoading(true);
      getCreativeWorkById(params.workId)
        .then((res) => {
          if (res.data) {
            setTitle(res.data.title);
            setContent(res.data.content);
            setType(res.data.type);
            setMood(res.data.mood);
          }
        })
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [params.workId]);

  const handleSave = async () => {
    if (!title.trim() && !content.trim()) {
      Alert.alert("Empty Work", "Please add a title or some content.");
      return;
    }

    setSaving(true);
    try {
      if (isEditing) {
        await updateCreativeWork(params.workId!, title, content, mood);
      } else {
        await createCreativeWork(type, title, content, mood);
      }
      router.back();
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to save work");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = () => {
    if (!params.workId) return;
    Alert.alert("Delete Work", "Are you sure you want to delete this work?", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Delete",
        style: "destructive",
        onPress: async () => {
          try {
            await deleteCreativeWork(params.workId!);
            router.back();
          } catch (err: any) {
            Alert.alert("Error", err.message ?? "Failed to delete work");
          }
        },
      },
    ]);
  };

  const handleShare = async () => {
    if (!params.workId) return;
    try {
      await shareCreativeWork(params.workId);
      Alert.alert("Shared", "Your work has been shared publicly.");
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to share work");
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

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        keyboardShouldPersistTaps="handled"
      >
        {/* Title Input */}
        <TextInput
          className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-lg font-bold text-warmgray-800 mb-3"
          placeholder="Title"
          placeholderTextColor="#A89885"
          value={title}
          onChangeText={setTitle}
        />

        {/* Type Picker */}
        <Text className="text-sm font-semibold text-warmgray-600 mb-2">
          Type
        </Text>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-4"
          contentContainerStyle={{ gap: 8 }}
        >
          {WORK_TYPES.map((t) => (
            <Pressable
              key={t}
              onPress={() => setType(t)}
              className="px-4 py-1.5 rounded-full"
              style={{
                backgroundColor: type === t ? "#3D2E22" : "#F5F0EB",
              }}
            >
              <Text
                className="text-sm font-semibold"
                style={{ color: type === t ? "#fff" : "#A89885" }}
              >
                {TYPE_LABELS[t]}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* Content Area */}
        <TextInput
          className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-base text-warmgray-800 mb-4"
          placeholder="Start writing..."
          placeholderTextColor="#A89885"
          value={content}
          onChangeText={setContent}
          multiline
          numberOfLines={12}
          textAlignVertical="top"
          style={{ minHeight: 240 }}
        />

        {/* Mood Picker */}
        <Text className="text-sm font-semibold text-warmgray-600 mb-2">
          Mood
        </Text>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-6"
          contentContainerStyle={{ gap: 8 }}
        >
          {MOODS.map((m) => (
            <Pressable
              key={m}
              onPress={() => setMood(m)}
              className="px-3 py-1.5 rounded-full flex-row items-center"
              style={{
                backgroundColor:
                  mood === m ? MOOD_COLORS[m] : MOOD_COLORS[m] + "25",
                borderWidth: mood === m ? 2 : 0,
                borderColor: MOOD_COLORS[m],
              }}
            >
              <View
                className="w-2.5 h-2.5 rounded-full mr-1.5"
                style={{ backgroundColor: MOOD_COLORS[m] }}
              />
              <Text
                className="text-sm font-medium"
                style={{
                  color: mood === m ? "#fff" : "#3D2E22",
                }}
              >
                {m}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* Action Buttons */}
        <Pressable
          onPress={handleSave}
          disabled={saving}
          className="bg-primary-500 rounded-xl py-3.5 items-center mb-3"
          style={({ pressed }) => ({ opacity: pressed || saving ? 0.7 : 1 })}
        >
          {saving ? (
            <ActivityIndicator size="small" color="#fff" />
          ) : (
            <Text className="text-white font-bold text-base">
              {isEditing ? "Update" : "Save"}
            </Text>
          )}
        </Pressable>

        {isEditing && (
          <View className="flex-row gap-3">
            <Pressable
              onPress={handleShare}
              className="flex-1 border border-primary-400 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-primary-500 font-semibold text-sm">Share</Text>
            </Pressable>
            <Pressable
              onPress={handleDelete}
              className="flex-1 border border-red-300 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-red-500 font-semibold text-sm">Delete</Text>
            </Pressable>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
