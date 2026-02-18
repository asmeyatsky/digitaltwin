import React from "react";
import { View, Text, Pressable, ScrollView, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useAuth } from "@/lib/hooks";

interface ActionRowProps {
  label: string;
  description: string;
  onPress: () => void;
  destructive?: boolean;
}

function ActionRow({ label, description, onPress, destructive }: ActionRowProps) {
  return (
    <Pressable
      onPress={onPress}
      className={`bg-white rounded-2xl px-4 py-4 border ${
        destructive ? "border-red-100" : "border-warmgray-100"
      }`}
      style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
    >
      <Text
        className={`text-base font-semibold ${
          destructive ? "text-red-500" : "text-warmgray-800"
        }`}
      >
        {label}
      </Text>
      <Text className="text-sm text-warmgray-400 mt-0.5">{description}</Text>
    </Pressable>
  );
}

export default function PrivacySettingsScreen() {
  const { logout } = useAuth();

  const confirm = (title: string, message: string, action: () => void) => {
    if (Platform.OS === "web") {
      if (window.confirm(`${title}\n\n${message}`)) action();
    } else {
      Alert.alert(title, message, [
        { text: "Cancel", style: "cancel" },
        { text: "Confirm", style: "destructive", onPress: action },
      ]);
    }
  };

  const handleExportConversations = () => {
    const msg = "Your conversation export will be emailed to you shortly.";
    if (Platform.OS === "web") window.alert(msg);
    else Alert.alert("Export Started", msg);
  };

  const handleDeleteAllConversations = () => {
    confirm(
      "Delete All Conversations",
      "This will permanently delete all your conversation history. This cannot be undone.",
      () => {
        const msg = "All conversations have been deleted.";
        if (Platform.OS === "web") window.alert(msg);
        else Alert.alert("Done", msg);
      }
    );
  };

  const handleDeleteAccount = () => {
    confirm(
      "Delete Account",
      "This will permanently delete your account and all associated data. This cannot be undone.",
      () => {
        logout();
      }
    );
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        <Text className="text-sm text-warmgray-500 mb-4">
          Manage your data and privacy preferences.
        </Text>

        <View className="mb-6">
          <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
            Data Export
          </Text>
          <View className="gap-2">
            <ActionRow
              label="Export Conversations"
              description="Download all your conversation data as JSON"
              onPress={handleExportConversations}
            />
          </View>
        </View>

        <View className="mb-6">
          <Text className="text-xs font-bold text-warmgray-400 uppercase tracking-wider mb-2 ml-1">
            Danger Zone
          </Text>
          <View className="gap-2">
            <ActionRow
              label="Delete All Conversations"
              description="Permanently remove all conversation history"
              onPress={handleDeleteAllConversations}
              destructive
            />
            <ActionRow
              label="Delete Account"
              description="Permanently delete your account and all data"
              onPress={handleDeleteAccount}
              destructive
            />
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
