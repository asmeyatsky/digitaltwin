import React from "react";
import { View, Text, ScrollView } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useSettingsStore } from "@/lib/store";

interface TraitSliderProps {
  label: string;
  description: string;
  value: number;
  leftLabel: string;
  rightLabel: string;
  onChange: (value: number) => void;
}

function TraitSlider({ label, description, value, leftLabel, rightLabel, onChange }: TraitSliderProps) {
  const steps = [0, 0.25, 0.5, 0.75, 1.0];

  return (
    <View className="bg-white rounded-2xl px-4 py-4 border border-warmgray-100">
      <Text className="text-base font-semibold text-warmgray-800">{label}</Text>
      <Text className="text-sm text-warmgray-400 mt-0.5 mb-3">{description}</Text>

      <View className="flex-row items-center justify-between mb-1.5">
        <Text className="text-xs text-warmgray-400">{leftLabel}</Text>
        <Text className="text-xs text-warmgray-400">{rightLabel}</Text>
      </View>

      {/* Custom step slider */}
      <View className="flex-row items-center h-8">
        <View className="flex-1 h-1 bg-warmgray-100 rounded-full flex-row relative">
          {/* Fill */}
          <View
            className="h-1 bg-primary-500 rounded-full absolute left-0 top-0"
            style={{ width: `${value * 100}%` }}
          />
          {/* Step dots */}
          {steps.map((step) => (
            <View
              key={step}
              onTouchEnd={() => onChange(step)}
              style={{
                position: "absolute",
                left: `${step * 100}%`,
                transform: [{ translateX: -10 }],
                width: 20,
                height: 20,
                alignItems: "center",
                justifyContent: "center",
                top: -10,
              }}
            >
              <View
                style={{
                  width: step === value ? 16 : 10,
                  height: step === value ? 16 : 10,
                  borderRadius: 8,
                  backgroundColor: step <= value ? "#FF8B47" : "#E8DDD2",
                  borderWidth: step === value ? 3 : 0,
                  borderColor: "#fff",
                }}
              />
            </View>
          ))}
        </View>
      </View>

      <Text className="text-xs text-primary-500 font-medium text-center mt-1">
        {Math.round(value * 100)}%
      </Text>
    </View>
  );
}

export default function PersonalitySettingsScreen() {
  const { personalityTraits, setPersonalityTraits } = useSettingsStore();

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        <Text className="text-sm text-warmgray-500 mb-4">
          Adjust your companion's communication style and personality.
        </Text>

        <View className="gap-3">
          <TraitSlider
            label="Friendliness"
            description="How warm and approachable your companion is"
            value={personalityTraits.friendliness}
            leftLabel="Professional"
            rightLabel="Very Friendly"
            onChange={(v) => setPersonalityTraits({ friendliness: v })}
          />

          <TraitSlider
            label="Humor"
            description="How often your companion uses humor"
            value={personalityTraits.humor}
            leftLabel="Serious"
            rightLabel="Playful"
            onChange={(v) => setPersonalityTraits({ humor: v })}
          />

          <TraitSlider
            label="Empathy"
            description="How emotionally attuned responses are"
            value={personalityTraits.empathy}
            leftLabel="Practical"
            rightLabel="Highly Empathetic"
            onChange={(v) => setPersonalityTraits({ empathy: v })}
          />

          <TraitSlider
            label="Formality"
            description="The level of formal language used"
            value={personalityTraits.formality}
            leftLabel="Casual"
            rightLabel="Formal"
            onChange={(v) => setPersonalityTraits({ formality: v })}
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
