import { Stack } from "expo-router";

export default function LifeLayout() {
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
      <Stack.Screen name="index" options={{ title: "My Life" }} />
      <Stack.Screen name="add-event" options={{ title: "Add Life Event" }} />
    </Stack>
  );
}
