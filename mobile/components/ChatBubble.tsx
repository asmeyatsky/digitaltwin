import React from "react";
import { View, Text } from "react-native";
import type { Message } from "@/lib/api";
import { getEmotionColor } from "@/lib/hooks";
import AudioPlayButton from "./AudioPlayButton";

interface ChatBubbleProps {
  message: Message;
}

export default function ChatBubble({ message }: ChatBubbleProps) {
  const isUser = message.role === "user";
  const emotionColor = message.emotion
    ? getEmotionColor(message.emotion.primary)
    : undefined;

  const timestamp = new Date(message.timestamp);
  const timeStr = timestamp.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });

  return (
    <View
      className={`mb-3 max-w-[82%] ${isUser ? "self-end" : "self-start"}`}
    >
      <View
        className={`rounded-3xl px-4 py-3 ${
          isUser
            ? "rounded-br-lg bg-primary-500"
            : "rounded-bl-lg bg-white border border-warmgray-200"
        }`}
        style={
          !isUser && emotionColor
            ? { borderLeftColor: emotionColor, borderLeftWidth: 3 }
            : undefined
        }
      >
        <Text
          className={`text-base leading-6 ${
            isUser ? "text-white" : "text-warmgray-800"
          }`}
        >
          {message.content}
        </Text>

        {!isUser && (
          <AudioPlayButton messageId={message.id} text={message.content} />
        )}
      </View>

      <View
        className={`flex-row items-center mt-1 gap-2 ${
          isUser ? "justify-end" : "justify-start"
        }`}
      >
        <Text className="text-xs text-warmgray-400">{timeStr}</Text>
        {message.emotion && !isUser && (
          <View
            className="rounded-full px-2 py-0.5"
            style={{ backgroundColor: emotionColor + "30" }}
          >
            <Text
              className="text-xs font-medium capitalize"
              style={{ color: emotionColor }}
            >
              {message.emotion.primary}
            </Text>
          </View>
        )}
      </View>
    </View>
  );
}
