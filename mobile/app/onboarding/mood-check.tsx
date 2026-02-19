import React, { useState } from "react";
import { View, Text, Pressable } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";

interface MoodOption {
  label: string;
  emoji: string;
}

const moods: MoodOption[] = [
  { label: "Happy", emoji: "\uD83D\uDE0A" },
  { label: "Sad", emoji: "\uD83D\uDE22" },
  { label: "Anxious", emoji: "\uD83D\uDE1F" },
  { label: "Angry", emoji: "\uD83D\uDE20" },
  { label: "Calm", emoji: "\uD83D\uDE0C" },
  { label: "Excited", emoji: "\uD83E\uDD29" },
  { label: "Surprised", emoji: "\uD83D\uDE32" },
  { label: "Neutral", emoji: "\uD83D\uDE10" },
];

export default function MoodCheckScreen() {
  const router = useRouter();
  const [selectedMood, setSelectedMood] = useState<string | null>(null);

  const handleContinue = () => {
    if (!selectedMood) return;
    router.push({
      pathname: "/onboarding/personalize",
      params: { mood: selectedMood },
    });
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <View className="flex-1 px-6 pt-12">
        {/* Header */}
        <Text className="text-3xl font-bold text-warmgray-800 text-center mb-3">
          How are you feeling right now?
        </Text>
        <Text className="text-base text-warmgray-500 text-center mb-10">
          This helps your companion understand you from the start
        </Text>

        {/* Mood Grid */}
        <View className="flex-row flex-wrap justify-center gap-4">
          {moods.map((mood) => {
            const isSelected = selectedMood === mood.label;
            return (
              <Pressable
                key={mood.label}
                onPress={() => setSelectedMood(mood.label)}
                className={`w-[42%] items-center rounded-2xl py-5 border-2 ${
                  isSelected
                    ? "bg-primary-50 border-primary-500"
                    : "bg-white border-warmgray-100"
                }`}
                style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
              >
                <Text style={{ fontSize: 36 }}>{mood.emoji}</Text>
                <Text
                  className={`text-base font-medium mt-2 ${
                    isSelected ? "text-primary-600" : "text-warmgray-700"
                  }`}
                >
                  {mood.label}
                </Text>
              </Pressable>
            );
          })}
        </View>
      </View>

      {/* Continue Button */}
      <View className="px-6 pb-8">
        <Pressable
          onPress={handleContinue}
          disabled={!selectedMood}
          className={`w-full rounded-2xl py-4 items-center ${
            selectedMood ? "bg-primary-500" : "bg-primary-300"
          }`}
          style={({ pressed }) => ({
            opacity: pressed && selectedMood ? 0.85 : 1,
          })}
        >
          <Text className="text-white text-base font-semibold">
            Continue
          </Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}
