import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  Alert,
  RefreshControl,
  Switch,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useLocalSearchParams } from "expo-router";
import type { CommunityPost, CommunityReply } from "@/lib/api";
import {
  getCommunityPostById,
  replyToCommunityPost,
  likeCommunityPost,
  likeCommunityReply,
} from "@/lib/api";

function ReplyCard({
  reply,
  onLike,
}: {
  reply: CommunityReply;
  onLike: () => void;
}) {
  return (
    <View className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-2">
      <Text className="text-sm text-warmgray-700 mb-2">{reply.content}</Text>
      <View className="flex-row items-center justify-between">
        <Text className="text-xs text-warmgray-400">
          {reply.isAnonymous
            ? "Anonymous"
            : reply.authorUserId.substring(0, 8) + "..."}
        </Text>
        <Pressable
          onPress={onLike}
          className="flex-row items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.6 : 1 })}
        >
          <Text className="text-xs text-warmgray-400 mr-1">
            {reply.likeCount}
          </Text>
          <Text className="text-xs text-warmgray-400">Like</Text>
        </Pressable>
      </View>
    </View>
  );
}

export default function PostDetailScreen() {
  const { postId } = useLocalSearchParams<{ postId: string }>();
  const [post, setPost] = useState<CommunityPost | null>(null);
  const [replies, setReplies] = useState<CommunityReply[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [replyText, setReplyText] = useState("");
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const fetchData = useCallback(async () => {
    if (!postId) return;
    try {
      const res = await getCommunityPostById(postId);
      setPost(res.data?.post ?? null);
      setReplies(res.data?.replies ?? []);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [postId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchData();
    setRefreshing(false);
  };

  const handleReply = async () => {
    if (!postId || !replyText.trim()) return;
    setSubmitting(true);
    try {
      await replyToCommunityPost(postId, replyText.trim(), isAnonymous);
      setReplyText("");
      setIsAnonymous(false);
      await fetchData();
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to post reply");
    } finally {
      setSubmitting(false);
    }
  };

  const handleLikePost = async () => {
    if (!postId) return;
    try {
      await likeCommunityPost(postId);
      await fetchData();
    } catch {
      // ignore
    }
  };

  const handleLikeReply = async (replyId: string) => {
    try {
      await likeCommunityReply(replyId);
      await fetchData();
    } catch {
      // ignore
    }
  };

  if (loading) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <ActivityIndicator size="large" color="#FF8B47" />
      </SafeAreaView>
    );
  }

  if (!post) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <Text className="text-warmgray-500">Post not found</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <KeyboardAvoidingView
        className="flex-1"
        behavior={Platform.OS === "ios" ? "padding" : undefined}
        keyboardVerticalOffset={90}
      >
        <ScrollView
          contentContainerStyle={{ padding: 16, paddingBottom: 16 }}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
          }
        >
          {/* Post Content */}
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4">
            <Text className="text-xl font-bold text-warmgray-800 mb-2">
              {post.title}
            </Text>
            <Text className="text-sm text-warmgray-600 mb-4 leading-5">
              {post.content}
            </Text>
            <View className="flex-row items-center justify-between">
              <Text className="text-xs text-warmgray-400">
                {post.isAnonymous
                  ? "Anonymous"
                  : post.authorUserId.substring(0, 8) + "..."}
              </Text>
              <View className="flex-row items-center gap-4">
                <Pressable
                  onPress={handleLikePost}
                  className="flex-row items-center"
                  style={({ pressed }) => ({ opacity: pressed ? 0.6 : 1 })}
                >
                  <Text className="text-sm text-warmgray-500 font-semibold">
                    {post.likeCount} Like{post.likeCount !== 1 ? "s" : ""}
                  </Text>
                </Pressable>
                <Text className="text-xs text-warmgray-400">
                  {post.replyCount} repl{post.replyCount !== 1 ? "ies" : "y"}
                </Text>
              </View>
            </View>
          </View>

          {/* Replies */}
          <Text className="text-lg font-bold text-warmgray-800 mb-3">
            Replies
          </Text>
          {replies.length > 0 ? (
            replies.map((reply) => (
              <ReplyCard
                key={reply.id}
                reply={reply}
                onLike={() => handleLikeReply(reply.id)}
              />
            ))
          ) : (
            <View className="items-center py-6">
              <Text className="text-warmgray-400 text-sm">
                No replies yet. Start the conversation!
              </Text>
            </View>
          )}
        </ScrollView>

        {/* Reply Input */}
        <View className="border-t border-warmgray-100 bg-white px-4 py-3">
          <View className="flex-row items-center mb-2">
            <Text className="text-xs text-warmgray-500 mr-2">Anonymous</Text>
            <Switch
              value={isAnonymous}
              onValueChange={setIsAnonymous}
              trackColor={{ false: "#E5DDD5", true: "#FF8B47" }}
              thumbColor="#fff"
            />
          </View>
          <View className="flex-row items-end gap-2">
            <TextInput
              className="flex-1 border border-warmgray-200 rounded-xl px-4 py-2.5 text-base text-warmgray-800"
              placeholder="Write a reply..."
              placeholderTextColor="#A89885"
              value={replyText}
              onChangeText={setReplyText}
              multiline
              maxLength={2000}
            />
            <Pressable
              onPress={handleReply}
              disabled={submitting || !replyText.trim()}
              className="bg-primary-500 rounded-xl px-5 py-2.5 items-center justify-center"
              style={({ pressed }) => ({
                opacity: pressed || submitting || !replyText.trim() ? 0.6 : 1,
              })}
            >
              {submitting ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text className="text-white font-bold text-sm">Send</Text>
              )}
            </Pressable>
          </View>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
