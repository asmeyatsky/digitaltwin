import React, { useState } from "react";
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  ActivityIndicator,
  Alert,
  Platform,
  Switch,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import {
  addLifeEvent,
  type LifeEventCategory,
  type EmotionType,
} from "@/lib/api";

const CATEGORIES: LifeEventCategory[] = [
  "Career",
  "Relationship",
  "Health",
  "Education",
  "Milestone",
  "Loss",
  "Achievement",
  "Travel",
];

const EMOTIONS: EmotionType[] = [
  "Neutral",
  "Happy",
  "Sad",
  "Angry",
  "Anxious",
  "Surprised",
  "Calm",
  "Excited",
];

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

export default function AddEventScreen() {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dateStr, setDateStr] = useState(
    new Date().toISOString().slice(0, 10)
  );
  const [category, setCategory] = useState<LifeEventCategory>("Milestone");
  const [emotionalImpact, setEmotionalImpact] =
    useState<EmotionType>("Neutral");
  const [isRecurring, setIsRecurring] = useState(false);
  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    if (!title.trim()) {
      const msg = "Please enter a title for the event.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Missing Title", msg);
      return;
    }

    setSaving(true);
    try {
      await addLifeEvent({
        title: title.trim(),
        description: description.trim(),
        eventDate: new Date(dateStr).toISOString(),
        category,
        emotionalImpact,
        isRecurring,
      });
      router.back();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save event.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Error", msg);
    } finally {
      setSaving(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        {/* Title */}
        <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-1 ml-1">
          Title
        </Text>
        <TextInput
          value={title}
          onChangeText={setTitle}
          placeholder="e.g. Started new job"
          placeholderTextColor="#A8A29E"
          className="bg-white rounded-2xl px-4 py-3 text-base text-warmgray-800 border border-warmgray-100 mb-4"
        />

        {/* Description */}
        <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-1 ml-1">
          Description
        </Text>
        <TextInput
          value={description}
          onChangeText={setDescription}
          placeholder="Optional details about this event..."
          placeholderTextColor="#A8A29E"
          multiline
          numberOfLines={4}
          className="bg-white rounded-2xl px-4 py-3 text-base text-warmgray-800 border border-warmgray-100 mb-4"
          style={{ minHeight: 100, textAlignVertical: "top" }}
        />

        {/* Date */}
        <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-1 ml-1">
          Date
        </Text>
        <TextInput
          value={dateStr}
          onChangeText={setDateStr}
          placeholder="YYYY-MM-DD"
          placeholderTextColor="#A8A29E"
          className="bg-white rounded-2xl px-4 py-3 text-base text-warmgray-800 border border-warmgray-100 mb-4"
          keyboardType="default"
        />

        {/* Category */}
        <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
          Category
        </Text>
        <View className="flex-row flex-wrap gap-2 mb-4">
          {CATEGORIES.map((cat) => {
            const isSelected = category === cat;
            const color = CATEGORY_COLORS[cat];
            return (
              <Pressable
                key={cat}
                onPress={() => setCategory(cat)}
                style={{
                  backgroundColor: isSelected ? color + "20" : "#FFFFFF",
                  borderColor: isSelected ? color : "#E7E5E4",
                  borderWidth: 1,
                  borderRadius: 999,
                  paddingHorizontal: 14,
                  paddingVertical: 6,
                }}
              >
                <Text
                  style={{
                    color: isSelected ? color : "#78716C",
                    fontSize: 13,
                    fontWeight: isSelected ? "600" : "400",
                  }}
                >
                  {cat}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {/* Emotional Impact */}
        <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
          Emotional Impact
        </Text>
        <View className="flex-row flex-wrap gap-2 mb-4">
          {EMOTIONS.map((emotion) => {
            const isSelected = emotionalImpact === emotion;
            return (
              <Pressable
                key={emotion}
                onPress={() => setEmotionalImpact(emotion)}
                style={{
                  backgroundColor: isSelected ? "#FF8B4720" : "#FFFFFF",
                  borderColor: isSelected ? "#FF8B47" : "#E7E5E4",
                  borderWidth: 1,
                  borderRadius: 999,
                  paddingHorizontal: 14,
                  paddingVertical: 6,
                }}
              >
                <Text
                  style={{
                    color: isSelected ? "#FF8B47" : "#78716C",
                    fontSize: 13,
                    fontWeight: isSelected ? "600" : "400",
                  }}
                >
                  {emotion}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {/* Recurring Toggle */}
        <View className="flex-row items-center justify-between bg-white rounded-2xl px-4 py-3 border border-warmgray-100 mb-6">
          <View>
            <Text className="text-base font-semibold text-warmgray-800">
              Recurring Event
            </Text>
            <Text className="text-sm text-warmgray-400 mt-0.5">
              Birthdays, anniversaries, etc.
            </Text>
          </View>
          <Switch
            value={isRecurring}
            onValueChange={setIsRecurring}
            trackColor={{ false: "#D6D3D1", true: "#FF8B47" }}
            thumbColor="#FFFFFF"
          />
        </View>

        {/* Save Button */}
        <Pressable
          onPress={handleSave}
          disabled={saving}
          className="bg-primary-500 rounded-2xl py-4 items-center"
          style={({ pressed }) => ({ opacity: pressed || saving ? 0.7 : 1 })}
        >
          {saving ? (
            <ActivityIndicator color="#FFFFFF" />
          ) : (
            <Text className="text-white text-base font-semibold">
              Save Event
            </Text>
          )}
        </Pressable>
      </ScrollView>
    </SafeAreaView>
  );
}
