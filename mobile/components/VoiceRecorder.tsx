import React, { useRef, useState, useCallback } from "react";
import {
  View,
  Text,
  Pressable,
  Platform,
  ActivityIndicator,
} from "react-native";
import { Audio } from "expo-av";
import * as Haptics from "expo-haptics";

interface VoiceRecorderProps {
  onRecordingComplete: (audioBase64: string) => void;
  disabled?: boolean;
}

export default function VoiceRecorder({
  onRecordingComplete,
  disabled = false,
}: VoiceRecorderProps) {
  const [isRecording, setIsRecording] = useState(false);
  const [duration, setDuration] = useState(0);
  const recordingRef = useRef<Audio.Recording | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startRecording = useCallback(async () => {
    if (disabled) return;

    try {
      const permission = await Audio.requestPermissionsAsync();
      if (!permission.granted) {
        return;
      }

      await Audio.setAudioModeAsync({
        allowsRecordingIOS: true,
        playsInSilentModeIOS: true,
      });

      const { recording } = await Audio.Recording.createAsync(
        Audio.RecordingOptionsPresets.HIGH_QUALITY
      );

      recordingRef.current = recording;
      setIsRecording(true);
      setDuration(0);

      if (Platform.OS !== "web") {
        Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
      }

      timerRef.current = setInterval(() => {
        setDuration((d) => d + 1);
      }, 1000);
    } catch (err) {
      console.error("Failed to start recording:", err);
    }
  }, [disabled]);

  const stopRecording = useCallback(async () => {
    if (!recordingRef.current) return;

    try {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }

      setIsRecording(false);
      setDuration(0);

      await recordingRef.current.stopAndUnloadAsync();
      await Audio.setAudioModeAsync({ allowsRecordingIOS: false });

      const uri = recordingRef.current.getURI();
      recordingRef.current = null;

      if (uri) {
        // Read the file as base64
        const response = await fetch(uri);
        const blob = await response.blob();
        const reader = new FileReader();
        reader.onloadend = () => {
          const base64 = (reader.result as string)?.split(",")[1];
          if (base64) {
            onRecordingComplete(base64);
          }
        };
        reader.readAsDataURL(blob);
      }

      if (Platform.OS !== "web") {
        Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
      }
    } catch (err) {
      console.error("Failed to stop recording:", err);
    }
  }, [onRecordingComplete]);

  const formatDuration = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, "0")}`;
  };

  return (
    <View className="items-center justify-center">
      <Pressable
        onPressIn={startRecording}
        onPressOut={stopRecording}
        disabled={disabled}
        className={`w-12 h-12 rounded-full items-center justify-center ${
          disabled ? "opacity-40" : ""
        }`}
        style={{
          backgroundColor: isRecording ? "#E07A7A" : "#FF8B47",
        }}
      >
        {disabled ? (
          <ActivityIndicator color="#fff" size="small" />
        ) : (
          <View
            className={`${
              isRecording ? "w-4 h-4 rounded-sm" : "w-5 h-5 rounded-full"
            } bg-white`}
          />
        )}
      </Pressable>

      {isRecording && (
        <View className="absolute -top-8 bg-warmgray-800 rounded-full px-3 py-1">
          <Text className="text-white text-xs font-medium">
            {formatDuration(duration)}
          </Text>
        </View>
      )}

      <Text className="text-xs text-warmgray-400 mt-1">
        {isRecording ? "Release to send" : "Hold to record"}
      </Text>
    </View>
  );
}
