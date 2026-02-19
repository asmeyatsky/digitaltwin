import React, { useState, useRef, useEffect, useCallback } from "react";
import {
  View,
  Text,
  TextInput,
  Pressable,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  ActivityIndicator,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import ChatBubble from "@/components/ChatBubble";
import EmotionBadge from "@/components/EmotionBadge";
import VoiceRecorder from "@/components/VoiceRecorder";
import AvatarView from "@/components/AvatarView";
import Avatar3DView from "@/components/Avatar3DView";
import { useConversation, useEmotion, useAvatar, useCheckIn, getEmotionBgTint } from "@/lib/hooks";
import type { Message } from "@/lib/api";

export default function ChatScreen() {
  const [inputText, setInputText] = useState("");
  const flatListRef = useRef<FlatList<Message>>(null);
  const router = useRouter();

  const {
    messages,
    isSending,
    sendMessage,
    activeConversation,
  } = useConversation();

  const { currentEmotion } = useEmotion();
  const { avatarUrl: glbAvatarUrl } = useAvatar();
  const { pendingCheckIns, respond: respondToCheckIn } = useCheckIn();

  const bgTint = getEmotionBgTint(currentEmotion?.primary);

  const handleSend = useCallback(async () => {
    const text = inputText.trim();
    if (!text || isSending) return;

    setInputText("");
    try {
      await sendMessage({ content: text });
    } catch (err) {
      console.error("Send failed:", err);
    }
  }, [inputText, isSending, sendMessage]);

  const handleVoiceRecording = useCallback(
    async (audioBase64: string) => {
      try {
        await sendMessage({
          content: "[Voice message]",
          audioBase64,
        });
      } catch (err) {
        console.error("Voice send failed:", err);
      }
    },
    [sendMessage]
  );

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    if (messages.length > 0) {
      setTimeout(() => {
        flatListRef.current?.scrollToEnd({ animated: true });
      }, 100);
    }
  }, [messages.length]);

  const renderEmptyChat = () => (
    <View className="flex-1 items-center justify-center px-8">
      <AvatarView emotion={currentEmotion?.primary} size={96} />
      <Text className="text-2xl font-bold text-warmgray-800 mt-6 text-center">
        Hello there!
      </Text>
      <Text className="text-base text-warmgray-500 mt-2 text-center leading-6">
        I'm your Digital Twin Companion. Share how you're feeling, ask me
        anything, or just talk. I'm here for you.
      </Text>
      <View className="flex-row flex-wrap justify-center gap-2 mt-6">
        {["How are you?", "I'm feeling stressed", "Tell me something nice"].map(
          (suggestion) => (
            <Pressable
              key={suggestion}
              onPress={() => {
                setInputText(suggestion);
              }}
              className="bg-white border border-warmgray-200 rounded-full px-4 py-2"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-warmgray-600 text-sm">{suggestion}</Text>
            </Pressable>
          )
        )}
      </View>
    </View>
  );

  return (
    <SafeAreaView className="flex-1" style={{ backgroundColor: "#FDF8F3" }}>
      {/* Background tint overlay */}
      <View
        className="absolute inset-0"
        style={{ backgroundColor: bgTint }}
        pointerEvents="none"
      />

      {/* Header */}
      <View className="flex-row items-center justify-between px-4 py-3 border-b border-warmgray-100">
        <View className="flex-row items-center gap-3">
          {glbAvatarUrl ? (
            <Avatar3DView
              avatarUrl={glbAvatarUrl}
              emotion={currentEmotion?.primary}
              size={40}
            />
          ) : (
            <AvatarView
              emotion={currentEmotion?.primary}
              size={40}
              showRing={true}
            />
          )}
          <View>
            <Text className="text-lg font-bold text-warmgray-800">
              Companion
            </Text>
            <Text className="text-xs text-warmgray-400">
              {activeConversation ? "In conversation" : "Ready to chat"}
            </Text>
          </View>
        </View>

        <EmotionBadge emotion={currentEmotion} size="sm" />
      </View>

      {/* Check-in Banner */}
      {pendingCheckIns.length > 0 && (
        <View className="mx-4 mt-2 bg-primary-50 border border-primary-200 rounded-2xl px-4 py-3">
          <Text className="text-sm font-semibold text-primary-700 mb-1">
            Check-in
          </Text>
          <Text className="text-sm text-warmgray-700 leading-5">
            {pendingCheckIns[0].emotionContext ?? "How are you feeling today?"}
          </Text>
          <View className="flex-row gap-2 mt-2">
            <Pressable
              onPress={() => {
                respondToCheckIn({
                  id: pendingCheckIns[0].id,
                  response: "acknowledged",
                });
              }}
              className="bg-primary-500 rounded-full px-4 py-1.5"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-white text-xs font-medium">
                I'm doing okay
              </Text>
            </Pressable>
            <Pressable
              onPress={() => {
                setInputText("I'd like to talk about how I'm feeling");
              }}
              className="bg-white border border-primary-300 rounded-full px-4 py-1.5"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-primary-600 text-xs font-medium">
                Let's talk
              </Text>
            </Pressable>
          </View>
        </View>
      )}

      {/* Messages */}
      <KeyboardAvoidingView
        behavior={Platform.OS === "ios" ? "padding" : "height"}
        className="flex-1"
        keyboardVerticalOffset={Platform.OS === "ios" ? 90 : 0}
      >
        {messages.length === 0 ? (
          renderEmptyChat()
        ) : (
          <FlatList
            ref={flatListRef}
            data={messages}
            keyExtractor={(item) => item.id}
            renderItem={({ item }) => <ChatBubble message={item} />}
            contentContainerStyle={{
              paddingHorizontal: 16,
              paddingTop: 16,
              paddingBottom: 8,
            }}
            showsVerticalScrollIndicator={false}
            onContentSizeChange={() =>
              flatListRef.current?.scrollToEnd({ animated: false })
            }
          />
        )}

        {/* Typing indicator */}
        {isSending && (
          <View className="px-4 pb-2">
            <View className="self-start bg-white border border-warmgray-200 rounded-3xl rounded-bl-lg px-4 py-3 flex-row items-center gap-1.5">
              <View className="w-2 h-2 rounded-full bg-warmgray-300 animate-pulse" />
              <View className="w-2 h-2 rounded-full bg-warmgray-400 animate-pulse" />
              <View className="w-2 h-2 rounded-full bg-warmgray-300 animate-pulse" />
            </View>
          </View>
        )}

        {/* Input Area */}
        <View className="px-4 py-3 border-t border-warmgray-100 bg-white">
          <View className="flex-row items-end gap-2">
            <VoiceRecorder
              onRecordingComplete={handleVoiceRecording}
              disabled={isSending}
            />

            {/* Camera emotion detection */}
            <Pressable
              onPress={() => router.push("/camera/emotion")}
              className="w-9 h-9 rounded-full items-center justify-center bg-warmgray-50 border border-warmgray-200 mb-0.5"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <View style={{ width: 16, height: 14, borderWidth: 2, borderColor: "#A89885", borderRadius: 3 }}>
                <View style={{ width: 6, height: 6, borderRadius: 3, borderWidth: 1.5, borderColor: "#A89885", alignSelf: "center", marginTop: 1 }} />
              </View>
            </Pressable>

            <View className="flex-1 flex-row items-end bg-warmgray-50 rounded-3xl border border-warmgray-200 pl-4 pr-2 py-1">
              <TextInput
                className="flex-1 text-base text-warmgray-800 py-2.5 max-h-28"
                placeholder="Type a message..."
                placeholderTextColor="#A89885"
                multiline
                value={inputText}
                onChangeText={setInputText}
                editable={!isSending}
                onSubmitEditing={handleSend}
                blurOnSubmit={false}
              />

              <Pressable
                onPress={handleSend}
                disabled={!inputText.trim() || isSending}
                className={`w-9 h-9 rounded-full items-center justify-center mb-0.5 ${
                  inputText.trim() && !isSending
                    ? "bg-primary-500"
                    : "bg-warmgray-200"
                }`}
                style={({ pressed }) => ({
                  opacity: pressed ? 0.7 : 1,
                })}
              >
                {isSending ? (
                  <ActivityIndicator color="#fff" size="small" />
                ) : (
                  <View
                    style={{
                      width: 0,
                      height: 0,
                      borderLeftWidth: 8,
                      borderTopWidth: 6,
                      borderBottomWidth: 6,
                      borderLeftColor:
                        inputText.trim() ? "#fff" : "#A89885",
                      borderTopColor: "transparent",
                      borderBottomColor: "transparent",
                      marginLeft: 2,
                    }}
                  />
                )}
              </Pressable>
            </View>
          </View>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
