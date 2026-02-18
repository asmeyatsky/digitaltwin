import React from "react";
import { View, Text, Pressable, ScrollView, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import { useAvatar } from "@/lib/hooks";
import AvatarView from "@/components/AvatarView";
import Avatar3DView from "@/components/Avatar3DView";

export default function AvatarSettingsScreen() {
  const router = useRouter();
  const { avatarUrl, avatarId, deleteAvatar } = useAvatar();

  const handleRegenerate = () => {
    router.push("/avatar/setup");
  };

  const handleDelete = () => {
    const doDelete = async () => {
      try {
        await deleteAvatar();
      } catch (err) {
        const msg = "Failed to delete avatar. Please try again.";
        if (Platform.OS === "web") window.alert(msg);
        else Alert.alert("Error", msg);
      }
    };

    if (Platform.OS === "web") {
      if (window.confirm("Delete your avatar? This cannot be undone.")) {
        doDelete();
      }
    } else {
      Alert.alert("Delete Avatar", "Delete your avatar? This cannot be undone.", [
        { text: "Cancel", style: "cancel" },
        { text: "Delete", style: "destructive", onPress: doDelete },
      ]);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48, alignItems: "center" }}>
        {/* Current Avatar */}
        <View className="bg-white rounded-3xl p-8 border border-warmgray-100 items-center w-full mb-6">
          <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-4">
            Current Avatar
          </Text>

          {avatarUrl ? (
            <Avatar3DView
              avatarUrl={avatarUrl}
              size={160}
              interactive
            />
          ) : (
            <AvatarView size={160} />
          )}

          <Text className="text-sm text-warmgray-400 mt-4">
            {avatarId ? "3D Avatar Active" : "No custom avatar"}
          </Text>
        </View>

        {/* Actions */}
        <View className="w-full gap-3">
          <Pressable
            onPress={handleRegenerate}
            className="bg-primary-500 rounded-2xl py-4 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
          >
            <Text className="text-white font-bold text-base">
              {avatarId ? "Regenerate Avatar" : "Create Avatar"}
            </Text>
          </Pressable>

          {avatarId && (
            <Pressable
              onPress={handleDelete}
              className="bg-white border border-red-200 rounded-2xl py-4 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
            >
              <Text className="text-red-500 font-semibold text-base">
                Delete Avatar
              </Text>
            </Pressable>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
