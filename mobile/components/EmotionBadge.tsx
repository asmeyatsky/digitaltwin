import React from "react";
import { View, Text } from "react-native";
import type { EmotionResult } from "@/lib/api";
import { getEmotionColor } from "@/lib/hooks";

interface EmotionBadgeProps {
  emotion: EmotionResult | null;
  size?: "sm" | "md" | "lg";
}

const EMOTION_EMOJI: Record<string, string> = {
  joy: "😊",
  happiness: "😄",
  calm: "😌",
  sadness: "😢",
  anger: "😠",
  fear: "😰",
  surprise: "😮",
  love: "🥰",
  disgust: "😖",
  neutral: "😐",
};

export default function EmotionBadge({ emotion, size = "md" }: EmotionBadgeProps) {
  if (!emotion) return null;

  const color = getEmotionColor(emotion.primary);
  const emoji =
    EMOTION_EMOJI[emotion.primary.toLowerCase()] ?? EMOTION_EMOJI.neutral;

  const sizeClasses = {
    sm: "px-2 py-0.5",
    md: "px-3 py-1.5",
    lg: "px-4 py-2",
  };
  const textSizeClasses = {
    sm: "text-xs",
    md: "text-sm",
    lg: "text-base",
  };

  const confidencePercent = Math.round(emotion.confidence * 100);

  return (
    <View
      className={`flex-row items-center rounded-full ${sizeClasses[size]}`}
      style={{ backgroundColor: color + "25" }}
    >
      <Text className={textSizeClasses[size]}>{emoji}</Text>
      <Text
        className={`${textSizeClasses[size]} font-semibold capitalize ml-1`}
        style={{ color }}
      >
        {emotion.primary}
      </Text>
      {size !== "sm" && (
        <Text
          className={`${textSizeClasses[size]} ml-1 opacity-70`}
          style={{ color }}
        >
          {confidencePercent}%
        </Text>
      )}
    </View>
  );
}
