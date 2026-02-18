import React from "react";
import { View, Text, Image } from "react-native";
import { getEmotionColor } from "@/lib/hooks";

interface AvatarViewProps {
  avatarUrl?: string | null;
  emotion?: string;
  size?: number;
  showRing?: boolean;
  initials?: string;
}

export default function AvatarView({
  avatarUrl,
  emotion,
  size = 64,
  showRing = true,
  initials = "DT",
}: AvatarViewProps) {
  const emotionColor = getEmotionColor(emotion);
  const fontSize = size * 0.35;
  const ringWidth = Math.max(2, size * 0.05);

  return (
    <View
      className="items-center justify-center rounded-full"
      style={{
        width: size + ringWidth * 2 + 4,
        height: size + ringWidth * 2 + 4,
        borderWidth: showRing ? ringWidth : 0,
        borderColor: showRing ? emotionColor : "transparent",
        borderRadius: (size + ringWidth * 2 + 4) / 2,
      }}
    >
      {avatarUrl ? (
        <Image
          source={{ uri: avatarUrl }}
          style={{
            width: size,
            height: size,
            borderRadius: size / 2,
          }}
        />
      ) : (
        <View
          className="items-center justify-center"
          style={{
            width: size,
            height: size,
            borderRadius: size / 2,
            backgroundColor: emotionColor + "35",
          }}
        >
          <Text
            style={{
              fontSize,
              color: emotionColor,
              fontWeight: "700",
            }}
          >
            {initials}
          </Text>
        </View>
      )}
    </View>
  );
}
