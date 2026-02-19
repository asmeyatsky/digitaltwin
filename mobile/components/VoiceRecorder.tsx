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
import * as api from "../lib/api";

interface VoiceRecorderProps {
  onRecordingComplete: (audioBase64: string) => void;
  onTranscription?: (text: string) => void;
  disabled?: boolean;
}

const SILENCE_THRESHOLD_DB = -40;
const SILENCE_DURATION_MS = 2000;

export default function VoiceRecorder({
  onRecordingComplete,
  onTranscription,
  disabled = false,
}: VoiceRecorderProps) {
  const [isRecording, setIsRecording] = useState(false);
  const [duration, setDuration] = useState(0);
  const recordingRef = useRef<Audio.Recording | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const silenceStartRef = useRef<number | null>(null);

  const stopRecording = useCallback(async () => {
    if (!recordingRef.current) return;

    try {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }

      silenceStartRef.current = null;
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

        // Attempt STT transcription
        if (onTranscription) {
          try {
            const sttResult = await api.speechToText(uri);
            if (sttResult.success && sttResult.data?.text) {
              onTranscription(sttResult.data.text);
            }
          } catch (err) {
            console.warn("STT transcription failed:", err);
          }
        }
      }

      if (Platform.OS !== "web") {
        Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
      }
    } catch (err) {
      console.error("Failed to stop recording:", err);
    }
  }, [onRecordingComplete, onTranscription]);

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

      const { recording } = await Audio.Recording.createAsync({
        ...Audio.RecordingOptionsPresets.HIGH_QUALITY,
        isMeteringEnabled: true,
      });

      recordingRef.current = recording;
      setIsRecording(true);
      setDuration(0);
      silenceStartRef.current = null;

      if (Platform.OS !== "web") {
        Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
      }

      // Monitor metering for silence detection
      recording.setOnRecordingStatusUpdate((status) => {
        if (!status.isRecording) return;

        const metering = status.metering ?? -160;

        if (metering < SILENCE_THRESHOLD_DB) {
          if (silenceStartRef.current === null) {
            silenceStartRef.current = Date.now();
          } else if (Date.now() - silenceStartRef.current >= SILENCE_DURATION_MS) {
            // Auto-stop after sustained silence
            stopRecording();
          }
        } else {
          silenceStartRef.current = null;
        }
      });

      // Set metering update interval
      recording.setProgressUpdateInterval(200);

      timerRef.current = setInterval(() => {
        setDuration((d) => d + 1);
      }, 1000);
    } catch (err) {
      console.error("Failed to start recording:", err);
    }
  }, [disabled, stopRecording]);

  const toggleRecording = useCallback(() => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  }, [isRecording, startRecording, stopRecording]);

  const formatDuration = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, "0")}`;
  };

  return (
    <View className="items-center justify-center">
      <Pressable
        onPress={toggleRecording}
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
        {isRecording ? "Tap to stop" : "Tap to record"}
      </Text>
    </View>
  );
}
