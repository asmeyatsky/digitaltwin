import React, { useState } from "react";
import { View, Text, TextInput, Pressable, ScrollView } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useLocalSearchParams } from "expo-router";

const communicationStyles = [
  { id: "warm", label: "Warm & Supportive", emoji: "\uD83E\uDD17" },
  { id: "direct", label: "Direct & Honest", emoji: "\uD83C\uDFAF" },
  { id: "playful", label: "Playful & Lighthearted", emoji: "\uD83C\uDF89" },
];

const voiceOptions = [
  { id: "soft", label: "Soft" },
  { id: "energetic", label: "Energetic" },
  { id: "calm", label: "Calm" },
];

export default function PersonalizeScreen() {
  const router = useRouter();
  const { mood } = useLocalSearchParams<{ mood: string }>();
  const [companionName, setCompanionName] = useState("Luna");
  const [selectedStyle, setSelectedStyle] = useState<string>("warm");
  const [selectedVoice, setSelectedVoice] = useState<string>("soft");

  const handleContinue = () => {
    router.push({
      pathname: "/onboarding/permissions",
      params: {
        mood: mood ?? "",
        companionName,
        communicationStyle: selectedStyle,
        voicePreference: selectedVoice,
      },
    });
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <ScrollView
        contentContainerStyle={{ paddingHorizontal: 24, paddingTop: 48, paddingBottom: 120 }}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
      >
        {/* Header */}
        <Text className="text-3xl font-bold text-warmgray-800 text-center mb-2">
          Personalize Your Companion
        </Text>
        <Text className="text-base text-warmgray-500 text-center mb-10">
          Make it yours
        </Text>

        {/* Companion Name */}
        <View className="mb-8">
          <Text className="text-sm font-semibold text-warmgray-600 mb-2 ml-1">
            Companion Name
          </Text>
          <TextInput
            className="bg-white border border-warmgray-200 rounded-2xl px-4 py-3.5 text-base text-warmgray-800"
            placeholder="Enter a name"
            placeholderTextColor="#A89885"
            value={companionName}
            onChangeText={setCompanionName}
            autoCapitalize="words"
            autoCorrect={false}
          />
        </View>

        {/* Communication Style */}
        <View className="mb-8">
          <Text className="text-sm font-semibold text-warmgray-600 mb-3 ml-1">
            Communication Style
          </Text>
          <View className="gap-3">
            {communicationStyles.map((style) => {
              const isSelected = selectedStyle === style.id;
              return (
                <Pressable
                  key={style.id}
                  onPress={() => setSelectedStyle(style.id)}
                  className={`flex-row items-center rounded-2xl px-4 py-4 border-2 ${
                    isSelected
                      ? "bg-primary-50 border-primary-500"
                      : "bg-white border-warmgray-100"
                  }`}
                  style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
                >
                  <Text style={{ fontSize: 24 }} className="mr-3">
                    {style.emoji}
                  </Text>
                  <Text
                    className={`text-base font-medium ${
                      isSelected ? "text-primary-600" : "text-warmgray-700"
                    }`}
                  >
                    {style.label}
                  </Text>
                  {/* Radio indicator */}
                  <View className="ml-auto">
                    <View
                      className={`w-6 h-6 rounded-full border-2 items-center justify-center ${
                        isSelected
                          ? "border-primary-500"
                          : "border-warmgray-300"
                      }`}
                    >
                      {isSelected && (
                        <View className="w-3 h-3 rounded-full bg-primary-500" />
                      )}
                    </View>
                  </View>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Voice Preference */}
        <View className="mb-8">
          <Text className="text-sm font-semibold text-warmgray-600 mb-3 ml-1">
            Voice Preference
          </Text>
          <View className="flex-row gap-3">
            {voiceOptions.map((voice) => {
              const isSelected = selectedVoice === voice.id;
              return (
                <Pressable
                  key={voice.id}
                  onPress={() => setSelectedVoice(voice.id)}
                  className={`flex-1 items-center rounded-2xl py-4 border-2 ${
                    isSelected
                      ? "bg-primary-50 border-primary-500"
                      : "bg-white border-warmgray-100"
                  }`}
                  style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
                >
                  <Text
                    className={`text-base font-medium ${
                      isSelected ? "text-primary-600" : "text-warmgray-700"
                    }`}
                  >
                    {voice.label}
                  </Text>
                  {/* Radio dot */}
                  <View
                    className={`w-5 h-5 rounded-full border-2 items-center justify-center mt-2 ${
                      isSelected
                        ? "border-primary-500"
                        : "border-warmgray-300"
                    }`}
                  >
                    {isSelected && (
                      <View className="w-2.5 h-2.5 rounded-full bg-primary-500" />
                    )}
                  </View>
                </Pressable>
              );
            })}
          </View>
        </View>
      </ScrollView>

      {/* Continue Button */}
      <View className="absolute bottom-0 left-0 right-0 px-6 pb-8 pt-4 bg-companion-bg">
        <Pressable
          onPress={handleContinue}
          className="w-full bg-primary-500 rounded-2xl py-4 items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
        >
          <Text className="text-white text-base font-semibold">
            Continue
          </Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}
