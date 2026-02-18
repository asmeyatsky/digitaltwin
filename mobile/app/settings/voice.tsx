import React, { useState } from "react";
import { View, Text, Pressable, ScrollView, ActivityIndicator, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useVoice } from "@/lib/hooks";
import type { VoiceInfo } from "@/lib/api";

export default function VoiceSettingsScreen() {
  const { voices, isLoadingVoices, selectedVoiceId, selectVoice, cloneVoice, isCloning } = useVoice();
  const [previewingId, setPreviewingId] = useState<string | null>(null);

  const handlePreview = async (voice: VoiceInfo) => {
    if (!voice.previewUrl) return;
    setPreviewingId(voice.id);
    try {
      const { Audio } = await import("expo-av");
      const { sound } = await Audio.Sound.createAsync({ uri: voice.previewUrl });
      sound.setOnPlaybackStatusUpdate((status: any) => {
        if (status.didJustFinish) {
          setPreviewingId(null);
          sound.unloadAsync();
        }
      });
      await sound.playAsync();
    } catch {
      setPreviewingId(null);
    }
  };

  const handleClone = async () => {
    const msg = "Voice cloning requires uploading audio samples. This feature will use your microphone to record samples.";
    if (Platform.OS === "web") {
      window.alert(msg);
    } else {
      Alert.alert("Voice Cloning", msg);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        <Text className="text-sm text-warmgray-500 mb-4">
          Choose a voice for your companion or clone your own.
        </Text>

        {isLoadingVoices ? (
          <ActivityIndicator size="large" color="#FF8B47" className="mt-8" />
        ) : (
          <View className="gap-2">
            {voices.map((voice) => (
              <Pressable
                key={voice.id}
                onPress={() => selectVoice(voice.id)}
                className={`flex-row items-center justify-between bg-white rounded-2xl px-4 py-4 border ${
                  selectedVoiceId === voice.id ? "border-primary-500" : "border-warmgray-100"
                }`}
                style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
              >
                <View className="flex-1">
                  <View className="flex-row items-center gap-2">
                    <Text className="text-base font-semibold text-warmgray-800">
                      {voice.name}
                    </Text>
                    {voice.isCustom && (
                      <View className="bg-primary-50 rounded-full px-2 py-0.5">
                        <Text className="text-xs text-primary-600">Custom</Text>
                      </View>
                    )}
                  </View>
                  <Text className="text-sm text-warmgray-400 mt-0.5">
                    {voice.language} · {voice.gender}
                  </Text>
                </View>

                <View className="flex-row items-center gap-3">
                  {voice.previewUrl && (
                    <Pressable
                      onPress={() => handlePreview(voice)}
                      className="bg-warmgray-50 rounded-full px-3 py-1.5"
                    >
                      <Text className="text-xs text-warmgray-600">
                        {previewingId === voice.id ? "Playing..." : "Preview"}
                      </Text>
                    </Pressable>
                  )}
                  {selectedVoiceId === voice.id && (
                    <View className="w-5 h-5 rounded-full bg-primary-500 items-center justify-center">
                      <Text className="text-white text-xs">&#10003;</Text>
                    </View>
                  )}
                </View>
              </Pressable>
            ))}

            {voices.length === 0 && (
              <Text className="text-warmgray-400 text-center mt-8">
                No voices available. Check your connection and try again.
              </Text>
            )}
          </View>
        )}

        {/* Voice Cloning Section */}
        <View className="mt-8">
          <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
            Voice Cloning
          </Text>
          <Pressable
            onPress={handleClone}
            disabled={isCloning}
            className="bg-white rounded-2xl px-4 py-4 border border-warmgray-100"
            style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
          >
            <Text className="text-base font-semibold text-warmgray-800">
              Clone Your Voice
            </Text>
            <Text className="text-sm text-warmgray-400 mt-0.5">
              Create a custom voice based on your own recordings
            </Text>
          </Pressable>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
