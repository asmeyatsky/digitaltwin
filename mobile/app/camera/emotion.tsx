import React, { useRef, useState, useCallback, useEffect } from "react";
import { View, Text, Pressable, Platform } from "react-native";
import { CameraView, useCameraPermissions } from "expo-camera";
import { useRouter } from "expo-router";
import { useEmotionStore } from "@/lib/store";
import { getEmotionColor } from "@/lib/hooks";
import * as api from "@/lib/api";

const CAPTURE_INTERVAL_MS = 5000;

export default function EmotionCameraScreen() {
  const router = useRouter();
  const cameraRef = useRef<any>(null);
  const [permission, requestPermission] = useCameraPermissions();
  const [detectedEmotion, setDetectedEmotion] = useState<string | null>(null);
  const [confidence, setConfidence] = useState<number>(0);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const emotionStore = useEmotionStore();
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isAnalyzingRef = useRef(false);

  const captureAndAnalyze = useCallback(async () => {
    if (!cameraRef.current || isAnalyzingRef.current) return;

    try {
      isAnalyzingRef.current = true;
      setIsAnalyzing(true);

      const photo = await cameraRef.current.takePictureAsync({
        quality: 0.3,
        base64: false,
        skipProcessing: true,
      });

      const formData = new FormData();

      if (Platform.OS === "web") {
        const response = await fetch(photo.uri);
        const blob = await response.blob();
        formData.append("image", blob, "frame.jpg");
      } else {
        formData.append("image", {
          uri: photo.uri,
          type: "image/jpeg",
          name: "frame.jpg",
        } as any);
      }

      const result = await api.analyzeEmotion(formData);

      if (result.success && result.data) {
        setDetectedEmotion(result.data.emotion);
        setConfidence(result.data.confidence);
        emotionStore.pushEmotion({
          primary: result.data.emotion,
          confidence: result.data.confidence,
          valence: 0,
          arousal: 0,
        });
      }
    } catch (err) {
      console.error("Emotion capture failed:", err);
    } finally {
      isAnalyzingRef.current = false;
      setIsAnalyzing(false);
    }
  }, [emotionStore]);

  useEffect(() => {
    if (permission?.granted) {
      intervalRef.current = setInterval(captureAndAnalyze, CAPTURE_INTERVAL_MS);
    }
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [permission?.granted, captureAndAnalyze]);

  if (!permission) {
    return (
      <View className="flex-1 bg-black items-center justify-center">
        <Text className="text-white text-lg">Loading camera...</Text>
      </View>
    );
  }

  if (!permission.granted) {
    return (
      <View className="flex-1 bg-black items-center justify-center px-8">
        <Text className="text-white text-xl font-bold text-center mb-4">
          Camera Access Required
        </Text>
        <Text className="text-gray-300 text-base text-center mb-6">
          We need camera access to detect your facial emotions in real-time.
        </Text>
        <Pressable
          onPress={requestPermission}
          className="bg-primary-500 rounded-2xl px-8 py-4"
          style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
        >
          <Text className="text-white font-bold text-base">Grant Permission</Text>
        </Pressable>
        <Pressable
          onPress={() => router.back()}
          className="mt-4 py-3"
          style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
        >
          <Text className="text-gray-400 text-base">Go Back</Text>
        </Pressable>
      </View>
    );
  }

  const emotionColor = getEmotionColor(detectedEmotion ?? undefined);

  return (
    <View className="flex-1 bg-black">
      <CameraView
        ref={cameraRef}
        style={{ flex: 1 }}
        facing="front"
      >
        {/* Face framing guide */}
        <View className="flex-1 items-center justify-center">
          <View
            style={{
              width: 240,
              height: 320,
              borderRadius: 120,
              borderWidth: 2,
              borderColor: isAnalyzing ? "#FF8B47" : "rgba(255,255,255,0.4)",
              borderStyle: "dashed",
            }}
          />
        </View>

        {/* Emotion overlay */}
        {detectedEmotion && (
          <View className="absolute bottom-36 self-center items-center">
            <View
              className="rounded-full px-6 py-3"
              style={{ backgroundColor: emotionColor + "CC" }}
            >
              <Text className="text-white font-bold text-lg capitalize">
                {detectedEmotion}
              </Text>
              <Text className="text-white text-xs text-center mt-0.5">
                {Math.round(confidence * 100)}% confidence
              </Text>
            </View>
          </View>
        )}

        {/* Analyzing indicator */}
        {isAnalyzing && (
          <View className="absolute top-16 self-center bg-black/60 rounded-full px-4 py-2">
            <Text className="text-white text-xs">Analyzing...</Text>
          </View>
        )}

        {/* Close button */}
        <Pressable
          onPress={() => router.back()}
          className="absolute top-14 right-5 w-10 h-10 rounded-full bg-black/50 items-center justify-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
        >
          <Text className="text-white text-xl font-bold">&#10005;</Text>
        </Pressable>

        {/* Instructions */}
        <View className="absolute bottom-16 self-center">
          <Text className="text-white/70 text-sm text-center">
            Position your face in the oval
          </Text>
          <Text className="text-white/50 text-xs text-center mt-1">
            Emotions are detected every 5 seconds
          </Text>
        </View>
      </CameraView>
    </View>
  );
}
