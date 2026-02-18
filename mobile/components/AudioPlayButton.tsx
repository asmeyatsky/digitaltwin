import React from "react";
import { View, Text, Pressable, ActivityIndicator } from "react-native";
import { useTTS } from "@/lib/hooks";

interface AudioPlayButtonProps {
  messageId: string;
  text: string;
}

export default function AudioPlayButton({ messageId, text }: AudioPlayButtonProps) {
  const { isPlaying, playingMessageId, play, stop } = useTTS();
  const isThisPlaying = isPlaying && playingMessageId === messageId;

  const handlePress = () => {
    if (isThisPlaying) {
      stop();
    } else {
      play(messageId, text);
    }
  };

  return (
    <Pressable
      onPress={handlePress}
      className="flex-row items-center gap-2 mt-2 bg-warmgray-50 rounded-full px-3 py-1.5 self-start"
      style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
    >
      {isPlaying && playingMessageId === messageId && !isThisPlaying ? (
        <ActivityIndicator size="small" color="#A89885" />
      ) : (
        <View
          style={{
            width: 0,
            height: 0,
            borderLeftWidth: isThisPlaying ? 0 : 8,
            borderTopWidth: 6,
            borderBottomWidth: 6,
            borderLeftColor: "#FF8B47",
            borderTopColor: isThisPlaying ? undefined : "transparent",
            borderBottomColor: isThisPlaying ? undefined : "transparent",
            ...(isThisPlaying
              ? {
                  width: 10,
                  height: 12,
                  borderLeftWidth: 3,
                  borderRightWidth: 3,
                  borderTopWidth: 0,
                  borderBottomWidth: 0,
                  borderLeftColor: "#FF8B47",
                  borderRightColor: "#FF8B47",
                  borderTopColor: "transparent",
                  borderBottomColor: "transparent",
                }
              : {}),
          }}
        />
      )}
      <Text className="text-xs font-medium text-warmgray-500">
        {isThisPlaying ? "Playing..." : "Listen"}
      </Text>
    </Pressable>
  );
}
