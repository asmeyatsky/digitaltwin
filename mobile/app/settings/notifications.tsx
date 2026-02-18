import React from "react";
import { View, Text, Switch, ScrollView } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useSettingsStore } from "@/lib/store";

interface ToggleRowProps {
  label: string;
  description: string;
  value: boolean;
  onValueChange: (value: boolean) => void;
}

function ToggleRow({ label, description, value, onValueChange }: ToggleRowProps) {
  return (
    <View className="flex-row items-center justify-between bg-white rounded-2xl px-4 py-4 border border-warmgray-100">
      <View className="flex-1 mr-4">
        <Text className="text-base font-semibold text-warmgray-800">{label}</Text>
        <Text className="text-sm text-warmgray-400 mt-0.5">{description}</Text>
      </View>
      <Switch
        value={value}
        onValueChange={onValueChange}
        trackColor={{ false: "#E8DDD2", true: "#FF8B47" }}
        thumbColor="#fff"
      />
    </View>
  );
}

export default function NotificationsSettingsScreen() {
  const { notificationsEnabled, setNotificationsEnabled } = useSettingsStore();

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        <Text className="text-sm text-warmgray-500 mb-4">
          Control how and when you receive notifications.
        </Text>

        <View className="gap-2">
          <ToggleRow
            label="Push Notifications"
            description="Get notified about check-ins and insights"
            value={notificationsEnabled}
            onValueChange={setNotificationsEnabled}
          />

          <ToggleRow
            label="Daily Check-in Reminder"
            description="A gentle reminder to chat with your companion"
            value={notificationsEnabled}
            onValueChange={setNotificationsEnabled}
          />

          <ToggleRow
            label="Weekly Insights"
            description="Receive weekly emotional wellness summaries"
            value={notificationsEnabled}
            onValueChange={setNotificationsEnabled}
          />

          <ToggleRow
            label="New Features"
            description="Be notified about new features and updates"
            value={true}
            onValueChange={() => {}}
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
