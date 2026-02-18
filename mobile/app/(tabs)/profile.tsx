import React from "react";
import { View, Text, ScrollView, Pressable, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import AvatarView from "@/components/AvatarView";
import { useAuth, useEmotion } from "@/lib/hooks";

interface SettingsRowProps {
  label: string;
  description: string;
  onPress: () => void;
}

function SettingsRow({ label, description, onPress }: SettingsRowProps) {
  return (
    <Pressable
      onPress={onPress}
      className="flex-row items-center justify-between bg-white rounded-2xl px-4 py-4 border border-warmgray-100"
      style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
    >
      <View className="flex-1 mr-4">
        <Text className="text-base font-semibold text-warmgray-800">
          {label}
        </Text>
        <Text className="text-sm text-warmgray-400 mt-0.5">
          {description}
        </Text>
      </View>
      <Text className="text-warmgray-300 text-xl">›</Text>
    </Pressable>
  );
}

function SectionHeader({ title }: { title: string }) {
  return (
    <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
      {title}
    </Text>
  );
}

export default function ProfileScreen() {
  const { user, logout } = useAuth();
  const { currentEmotion } = useEmotion();
  const router = useRouter();

  const handleLogout = () => {
    if (Platform.OS === "web") {
      if (window.confirm("Are you sure you want to sign out?")) {
        logout();
      }
    } else {
      Alert.alert("Sign Out", "Are you sure you want to sign out?", [
        { text: "Cancel", style: "cancel" },
        {
          text: "Sign Out",
          style: "destructive",
          onPress: logout,
        },
      ]);
    }
  };

  const initials = user?.displayName
    ? user.displayName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .toUpperCase()
        .slice(0, 2)
    : "DT";

  const tierLabels: Record<string, string> = {
    free: "Free Plan",
    premium: "Premium",
    enterprise: "Enterprise",
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        showsVerticalScrollIndicator={false}
      >
        {/* Profile Card */}
        <View className="bg-white rounded-3xl p-6 border border-warmgray-100 items-center mb-6">
          <AvatarView
            avatarUrl={user?.avatarUrl}
            emotion={currentEmotion?.primary}
            size={88}
            initials={initials}
          />

          <Text className="text-xl font-bold text-warmgray-800 mt-4">
            {user?.displayName ?? "Digital Twin User"}
          </Text>
          <Text className="text-sm text-warmgray-400 mt-1">
            {user?.email ?? "user@example.com"}
          </Text>

          <View className="bg-primary-50 rounded-full px-3 py-1 mt-3">
            <Text className="text-xs font-semibold text-primary-600">
              {tierLabels[user?.subscriptionTier ?? "free"]}
            </Text>
          </View>

          {user?.createdAt && (
            <Text className="text-xs text-warmgray-300 mt-3">
              Member since{" "}
              {new Date(user.createdAt).toLocaleDateString(undefined, {
                month: "long",
                year: "numeric",
              })}
            </Text>
          )}
        </View>

        {/* Companion Settings */}
        <View className="mb-6">
          <SectionHeader title="Companion" />
          <View className="gap-2">
            <SettingsRow
              label="Voice Settings"
              description="Choose your companion's voice and speed"
              onPress={() => router.push("/settings/voice")}
            />
            <SettingsRow
              label="Avatar Customization"
              description="Personalize your companion's appearance"
              onPress={() => router.push("/settings/avatar")}
            />
            <SettingsRow
              label="Personality"
              description="Adjust communication style and tone"
              onPress={() => router.push("/settings/personality")}
            />
          </View>
        </View>

        {/* Account Settings */}
        <View className="mb-6">
          <SectionHeader title="Account" />
          <View className="gap-2">
            <SettingsRow
              label="Subscription"
              description="Manage your plan and billing"
              onPress={() => router.push("/settings/subscription")}
            />
            <SettingsRow
              label="Privacy & Data"
              description="Control your data and privacy settings"
              onPress={() => router.push("/settings/privacy")}
            />
            <SettingsRow
              label="Notifications"
              description="Configure push notifications"
              onPress={() => router.push("/settings/notifications")}
            />
          </View>
        </View>

        {/* Support */}
        <View className="mb-8">
          <SectionHeader title="Support" />
          <View className="gap-2">
            <SettingsRow
              label="Help Center"
              description="FAQs and guides"
              onPress={() => {}}
            />
            <SettingsRow
              label="Contact Support"
              description="Get help from our team"
              onPress={() => {}}
            />
            <SettingsRow
              label="About"
              description="Version 1.0.0"
              onPress={() => {}}
            />
          </View>
        </View>

        {/* Sign Out */}
        <Pressable
          onPress={handleLogout}
          className="bg-white rounded-2xl py-4 border border-red-200 items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
        >
          <Text className="text-red-500 font-semibold text-base">
            Sign Out
          </Text>
        </Pressable>

        <Text className="text-xs text-warmgray-300 text-center mt-4">
          Digital Twin Companion v1.0.0
        </Text>
      </ScrollView>
    </SafeAreaView>
  );
}
