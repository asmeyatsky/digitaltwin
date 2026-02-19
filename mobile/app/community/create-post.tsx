import React, { useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  TextInput,
  Alert,
  Switch,
  KeyboardAvoidingView,
  Platform,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useLocalSearchParams } from "expo-router";
import { createCommunityPost } from "@/lib/api";

export default function CreatePostScreen() {
  const router = useRouter();
  const { groupId } = useLocalSearchParams<{ groupId: string }>();
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const canSubmit = title.trim().length > 0 && content.trim().length > 0;

  const handleSubmit = async () => {
    if (!groupId || !canSubmit) return;
    setSubmitting(true);
    try {
      await createCommunityPost(groupId, title.trim(), content.trim(), isAnonymous);
      router.back();
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to create post");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <KeyboardAvoidingView
        className="flex-1"
        behavior={Platform.OS === "ios" ? "padding" : undefined}
        keyboardVerticalOffset={90}
      >
        <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
          <View className="mb-6">
            <Text className="text-2xl font-bold text-warmgray-800">
              Create Post
            </Text>
            <Text className="text-sm text-warmgray-500 mt-1">
              Share your thoughts with the community
            </Text>
          </View>

          {/* Title */}
          <View className="mb-4">
            <Text className="text-sm font-semibold text-warmgray-700 mb-2">
              Title
            </Text>
            <TextInput
              className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-base text-warmgray-800"
              placeholder="Give your post a title"
              placeholderTextColor="#A89885"
              value={title}
              onChangeText={setTitle}
              maxLength={300}
            />
          </View>

          {/* Content */}
          <View className="mb-4">
            <Text className="text-sm font-semibold text-warmgray-700 mb-2">
              Content
            </Text>
            <TextInput
              className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-base text-warmgray-800"
              placeholder="What would you like to share?"
              placeholderTextColor="#A89885"
              value={content}
              onChangeText={setContent}
              multiline
              numberOfLines={8}
              style={{ minHeight: 160, textAlignVertical: "top" }}
              maxLength={10000}
            />
          </View>

          {/* Anonymous Toggle */}
          <View className="flex-row items-center justify-between bg-white rounded-xl px-4 py-3 border border-warmgray-200 mb-6">
            <View>
              <Text className="text-base font-semibold text-warmgray-800">
                Post Anonymously
              </Text>
              <Text className="text-xs text-warmgray-500 mt-0.5">
                Your identity will be hidden from other members
              </Text>
            </View>
            <Switch
              value={isAnonymous}
              onValueChange={setIsAnonymous}
              trackColor={{ false: "#E5DDD5", true: "#FF8B47" }}
              thumbColor="#fff"
            />
          </View>

          {/* Submit Button */}
          <Pressable
            onPress={handleSubmit}
            disabled={submitting || !canSubmit}
            className="bg-primary-500 rounded-2xl py-3.5 items-center"
            style={({ pressed }) => ({
              opacity: pressed || submitting || !canSubmit ? 0.6 : 1,
            })}
          >
            {submitting ? (
              <ActivityIndicator color="#fff" size="small" />
            ) : (
              <Text className="text-white font-bold text-base">
                Publish Post
              </Text>
            )}
          </Pressable>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
