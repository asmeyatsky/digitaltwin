import React, { useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import type { CreativeWorkType } from "@/lib/api";
import { generateCreativePrompt } from "@/lib/api";

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

const TYPE_DESCRIPTIONS: Record<CreativeWorkType, string> = {
  Story: "Craft narratives from your experiences",
  Poem: "Express emotions through verse and rhythm",
  Reflection: "Look inward and explore your thoughts",
  Gratitude: "Appreciate the good in your life",
  Letter: "Write to someone or something meaningful",
  FreeWrite: "Let words flow without judgment",
};

const TYPE_COLORS: Record<CreativeWorkType, string> = {
  Story: "#3B82F6",
  Poem: "#8B5CF6",
  Reflection: "#10B981",
  Gratitude: "#F59E0B",
  Letter: "#EF4444",
  FreeWrite: "#6366F1",
};

export default function CreativePromptsScreen() {
  const router = useRouter();
  const [selectedType, setSelectedType] = useState<CreativeWorkType>("Story");
  const [prompt, setPrompt] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleGenerate = async () => {
    setLoading(true);
    setPrompt(null);
    try {
      const res = await generateCreativePrompt(selectedType);
      setPrompt(res.data?.prompt ?? null);
    } catch {
      setPrompt("Something went wrong. Try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleStartWriting = () => {
    router.push({
      pathname: "/creative/editor",
      params: {
        prefillContent: prompt ?? "",
        prefillType: selectedType,
      },
    });
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        {/* Header */}
        <View className="mb-6">
          <Text className="text-2xl font-bold text-warmgray-800">
            Creative Prompts
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Choose a writing type and get inspired
          </Text>
        </View>

        {/* Type Selection Grid */}
        <View className="flex-row flex-wrap gap-3 mb-6">
          {WORK_TYPES.map((t) => {
            const color = TYPE_COLORS[t];
            const isSelected = selectedType === t;

            return (
              <Pressable
                key={t}
                onPress={() => setSelectedType(t)}
                className="rounded-2xl p-4 border"
                style={{
                  width: "47%",
                  backgroundColor: isSelected ? color + "15" : "#fff",
                  borderColor: isSelected ? color : "#E5E0DB",
                  borderWidth: isSelected ? 2 : 1,
                }}
              >
                <Text
                  className="text-base font-bold mb-1"
                  style={{ color: isSelected ? color : "#3D2E22" }}
                >
                  {TYPE_LABELS[t]}
                </Text>
                <Text className="text-xs text-warmgray-500" numberOfLines={2}>
                  {TYPE_DESCRIPTIONS[t]}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {/* Generate Button */}
        <Pressable
          onPress={handleGenerate}
          disabled={loading}
          className="bg-warmgray-700 rounded-xl py-3.5 items-center mb-6"
          style={({ pressed }) => ({ opacity: pressed || loading ? 0.7 : 1 })}
        >
          {loading ? (
            <ActivityIndicator size="small" color="#fff" />
          ) : (
            <Text className="text-white font-bold text-base">
              Generate Prompt
            </Text>
          )}
        </Pressable>

        {/* Prompt Card */}
        {prompt && (
          <View className="bg-white rounded-2xl p-6 border border-warmgray-100 mb-4">
            <View className="flex-row items-center mb-3">
              <View
                className="px-2 py-0.5 rounded-lg"
                style={{
                  backgroundColor: (TYPE_COLORS[selectedType] ?? "#A89885") + "20",
                }}
              >
                <Text
                  className="text-xs font-semibold"
                  style={{ color: TYPE_COLORS[selectedType] }}
                >
                  {TYPE_LABELS[selectedType]}
                </Text>
              </View>
            </View>
            <Text className="text-lg text-warmgray-800 leading-7 mb-4">
              {prompt}
            </Text>
            <Pressable
              onPress={handleStartWriting}
              className="bg-primary-500 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-white font-bold text-base">
                Start Writing
              </Text>
            </Pressable>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
