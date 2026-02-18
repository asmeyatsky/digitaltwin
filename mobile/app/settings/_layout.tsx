import { Stack } from "expo-router";

export default function SettingsLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: true,
        headerStyle: { backgroundColor: "#FDF8F3" },
        headerTintColor: "#3D2E22",
        headerBackTitle: "Back",
        contentStyle: { backgroundColor: "#FDF8F3" },
      }}
    >
      <Stack.Screen name="voice" options={{ title: "Voice Settings" }} />
      <Stack.Screen name="avatar" options={{ title: "Avatar" }} />
      <Stack.Screen name="personality" options={{ title: "Personality" }} />
      <Stack.Screen name="notifications" options={{ title: "Notifications" }} />
      <Stack.Screen name="privacy" options={{ title: "Privacy & Data" }} />
      <Stack.Screen name="subscription" options={{ title: "Subscription" }} />
    </Stack>
  );
}
